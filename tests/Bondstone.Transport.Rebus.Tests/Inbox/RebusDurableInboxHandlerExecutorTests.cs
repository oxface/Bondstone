using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Inbox;

public sealed class RebusDurableInboxHandlerExecutorTests
{
    private static readonly DateTimeOffset ReceivedAtUtc =
        DateTimeOffset.Parse("2026-06-05T12:00:00+00:00");

    private static readonly DateTimeOffset ProcessedAtUtc =
        DateTimeOffset.Parse("2026-06-05T12:05:00+00:00");

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenCommandIsHandled_MapsEnvelopeAndInboxKey()
    {
        RebusDurableMessageEnvelope envelope = CreateEnvelope();
        var coreExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var executor = new RebusDurableInboxHandlerExecutor(
            coreExecutor,
            new FixedTimeProvider(ReceivedAtUtc));
        DurableMessageEnvelope? handledEnvelope = null;
        var commitCalls = 0;

        DurableInboxHandleResult result = await executor.HandleOnceAsync(
            envelope,
            " fulfillment-handler ",
            (message, _) =>
            {
                handledEnvelope = message;
                return ValueTask.CompletedTask;
            },
            _ =>
            {
                commitCalls++;
                return ValueTask.CompletedTask;
            });

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.Equal(1, commitCalls);
        Assert.NotNull(handledEnvelope);
        Assert.Equal(envelope.MessageId, handledEnvelope.MessageId);
        Assert.Equal(MessageKind.Command, handledEnvelope.MessageKind);
        Assert.Equal(envelope.MessageTypeName, handledEnvelope.MessageTypeName);
        Assert.Equal(envelope.SourceModule, handledEnvelope.SourceModule);
        Assert.Equal(envelope.TargetModule, handledEnvelope.TargetModule);
        Assert.Equal(envelope.Payload, handledEnvelope.Payload);
        Assert.Equal(envelope.Metadata, handledEnvelope.Metadata);
        Assert.Equal(envelope.CreatedAtUtc, handledEnvelope.CreatedAtUtc);
        Assert.Equal(envelope.DurableOperationId, handledEnvelope.DurableOperationId);
        Assert.Equal(envelope.TraceParent, handledEnvelope.TraceContext?.TraceParent);
        Assert.Equal(envelope.TraceState, handledEnvelope.TraceContext?.TraceState);
        Assert.Equal(envelope.TraceBaggage, handledEnvelope.TraceContext?.Baggage);
        Assert.Equal(envelope.CausationId, handledEnvelope.CausationId);
        Assert.Equal(envelope.PartitionKey, handledEnvelope.PartitionKey);

        Assert.NotNull(coreExecutor.Record);
        Assert.Equal(envelope.MessageId, coreExecutor.Record.Key.MessageId);
        Assert.Equal("fulfillment", coreExecutor.Record.Key.ModuleName);
        Assert.Equal("fulfillment-handler", coreExecutor.Record.Key.HandlerIdentity);
        Assert.Equal(ReceivedAtUtc, coreExecutor.Record.ReceivedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenTraceParentIsMissing_DoesNotCreateTraceContext()
    {
        var coreExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var executor = new RebusDurableInboxHandlerExecutor(
            coreExecutor,
            new FixedTimeProvider(ReceivedAtUtc));
        DurableMessageEnvelope? handledEnvelope = null;

        await executor.HandleOnceAsync(
            CreateEnvelope(traceParent: null),
            "fulfillment-handler",
            (message, _) =>
            {
                handledEnvelope = message;
                return ValueTask.CompletedTask;
            },
            _ => ValueTask.CompletedTask);

        Assert.NotNull(handledEnvelope);
        Assert.Null(handledEnvelope.TraceContext);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenTraceParentIsInvalid_Throws()
    {
        var coreExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var executor = new RebusDurableInboxHandlerExecutor(
            coreExecutor,
            new FixedTimeProvider(ReceivedAtUtc));

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await executor.HandleOnceAsync(
                CreateEnvelope(traceParent: "legacy-correlation-id"),
                "fulfillment-handler",
                (_, _) => ValueTask.CompletedTask,
                _ => ValueTask.CompletedTask));

        Assert.Null(coreExecutor.Record);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenAlreadyProcessed_CompletesNormally()
    {
        var coreExecutor = new CapturingInboxHandlerExecutor(
            DurableInboxHandleStatus.AlreadyProcessed);
        var executor = new RebusDurableInboxHandlerExecutor(
            coreExecutor,
            new FixedTimeProvider(ReceivedAtUtc));

        DurableInboxHandleResult result = await executor.HandleOnceAsync(
            CreateEnvelope(),
            "fulfillment-handler",
            (_, _) => throw new InvalidOperationException("Handler should not run."),
            _ => throw new InvalidOperationException("Commit should not run."));

        Assert.Equal(DurableInboxHandleStatus.AlreadyProcessed, result.Status);
        Assert.Equal(0, coreExecutor.HandlerCalls);
        Assert.Equal(0, coreExecutor.CommitCalls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenAlreadyReceived_ThrowsReceiveException()
    {
        var coreExecutor = new CapturingInboxHandlerExecutor(
            DurableInboxHandleStatus.AlreadyReceived);
        var executor = new RebusDurableInboxHandlerExecutor(
            coreExecutor,
            new FixedTimeProvider(ReceivedAtUtc));

        RebusDurableInboxAlreadyReceivedException exception =
            await Assert.ThrowsAsync<RebusDurableInboxAlreadyReceivedException>(
                async () => await executor.HandleOnceAsync(
                    CreateEnvelope(),
                    "fulfillment-handler",
                    (_, _) => throw new InvalidOperationException("Handler should not run."),
                    _ => throw new InvalidOperationException("Commit should not run.")));

        Assert.Equal(DurableInboxHandleStatus.AlreadyReceived, exception.Result.Status);
        Assert.Equal("fulfillment-handler", exception.Result.Record.Key.HandlerIdentity);
        Assert.Contains("already received", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenEnvelopeIsEvent_Throws()
    {
        var coreExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var executor = new RebusDurableInboxHandlerExecutor(coreExecutor);

        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await executor.HandleOnceAsync(
                CreateEnvelope(messageKind: MessageKind.Event, targetModule: null),
                "fulfillment-handler",
                (_, _) => ValueTask.CompletedTask,
                _ => ValueTask.CompletedTask));

        Assert.Null(coreExecutor.Record);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenCommandHasNoTargetModule_Throws()
    {
        var coreExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var executor = new RebusDurableInboxHandlerExecutor(coreExecutor);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await executor.HandleOnceAsync(
                CreateEnvelope(targetModule: null),
                "fulfillment-handler",
                (_, _) => ValueTask.CompletedTask,
                _ => ValueTask.CompletedTask));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenArgumentsAreNull_Throws()
    {
        var executor = new RebusDurableInboxHandlerExecutor(
            new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled));

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await executor.HandleOnceAsync(
                null!,
                "fulfillment-handler",
                (_, _) => ValueTask.CompletedTask,
                _ => ValueTask.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await executor.HandleOnceAsync(
                CreateEnvelope(),
                "fulfillment-handler",
                null!,
                _ => ValueTask.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await executor.HandleOnceAsync(
                CreateEnvelope(),
                "fulfillment-handler",
                (_, _) => ValueTask.CompletedTask,
                null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneRebusInbox_RegistersReceiveAdapter()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDurableInboxHandlerExecutor>(
            new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled));

        services.AddBondstoneRebusInbox();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Assert.IsType<RebusDurableInboxHandlerExecutor>(
            serviceProvider.GetRequiredService<IRebusDurableInboxHandlerExecutor>());
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusTypedCommandReceivePipeline));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusInbox_WhenUsedInBondstoneBuilder_RegistersReceiveAdapter()
    {
        var services = new ServiceCollection();

        services.AddBondstone(builder => builder.UseRebusInbox());

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusDurableInboxHandlerExecutor)
                && descriptor.ImplementationType == typeof(RebusDurableInboxHandlerExecutor));
    }

    private static RebusDurableMessageEnvelope CreateEnvelope(
        MessageKind messageKind = MessageKind.Command,
        string? targetModule = "fulfillment",
        string? traceParent = "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00")
    {
        return new RebusDurableMessageEnvelope(
            Guid.Parse("b17d395f-d458-4b67-aa41-aa464ad16fe7"),
            messageKind.ToString(),
            "fulfillment.order.reserve.v1",
            "sales",
            targetModule,
            """{"orderId":"A-100"}""",
            """{"schema":"fulfillment.order.reserve"}""",
            DateTimeOffset.Parse("2026-06-05T11:55:00+00:00"),
            Guid.Parse("a053747e-d1a5-46c3-b488-fc9bc6f032d0"),
            traceParent,
            "state=value",
            "tenant=sales",
            Guid.Parse("66ebbd2f-069f-4427-94fd-19e77bdbca86"),
            "orders/A-100");
    }

    private sealed class CapturingInboxHandlerExecutor(DurableInboxHandleStatus status)
        : IDurableInboxHandlerExecutor
    {
        public DurableInboxRecord? Record { get; private set; }

        public int HandlerCalls { get; private set; }

        public int CommitCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            Func<CancellationToken, ValueTask> commit,
            CancellationToken ct = default)
        {
            Record = record;

            if (status == DurableInboxHandleStatus.Handled)
            {
                HandlerCalls++;
                await handler(ct);
                CommitCalls++;
                await commit(ct);
            }

            DurableInboxRecord resultRecord = status == DurableInboxHandleStatus.AlreadyProcessed
                ? record.MarkProcessed(ProcessedAtUtc)
                : record;

            return new DurableInboxHandleResult(status, resultRecord);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
