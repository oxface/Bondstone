using System.Diagnostics;
using System.Text.Json;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Inbox;

public sealed class RebusTypedCommandReceivePipelineTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenCommandIsRegistered_DeserializesStartsActivityAndDelegates()
    {
        var registry = new MessageTypeRegistry();
        registry.Register<ReserveOrderCommand>("fulfillment.order.reserve.v1");
        var inboxExecutor = new CapturingRebusInboxExecutor(DurableInboxHandleStatus.Handled);
        var pipeline = new RebusTypedCommandReceivePipeline(registry, inboxExecutor);
        List<Activity> stoppedActivities = [];
        using ActivityListener listener = Listen(stoppedActivities);
        ReserveOrderCommand? handledCommand = null;
        Activity? handlerActivity = null;
        var commitCalls = 0;

        DurableInboxHandleResult result = await pipeline.HandleOnceAsync<ReserveOrderCommand>(
            CreateEnvelope(),
            " reserve-order-handler ",
            (command, _) =>
            {
                handledCommand = command;
                handlerActivity = Activity.Current;
                return ValueTask.CompletedTask;
            },
            _ =>
            {
                commitCalls++;
                return ValueTask.CompletedTask;
            });

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.Equal("A-100", handledCommand?.OrderId);
        Assert.NotNull(handlerActivity);
        Assert.Equal("bondstone.rebus.command.receive", handlerActivity.OperationName);
        Assert.Equal(CreateEnvelope().MessageId, inboxExecutor.Envelope?.MessageId);
        Assert.Equal("reserve-order-handler", inboxExecutor.HandlerIdentity);
        Assert.Equal(1, inboxExecutor.HandlerCalls);
        Assert.Equal(1, commitCalls);

        Activity activity = Assert.Single(
            Snapshot(stoppedActivities),
            activity => activity.OperationName == "bondstone.rebus.command.receive"
                && string.Equals(
                    GetTag(activity, "bondstone.message_id"),
                    CreateEnvelope().MessageId.ToString("D"),
                    StringComparison.Ordinal));
        Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", activity.TraceId.ToHexString());
        Assert.Equal("00f067aa0ba902b7", activity.ParentSpanId.ToHexString());
        Assert.Equal("rebus", GetTag(activity, "bondstone.transport"));
        Assert.Equal(CreateEnvelope().MessageId.ToString("D"), GetTag(activity, "bondstone.message_id"));
        Assert.Equal("fulfillment.order.reserve.v1", GetTag(activity, "bondstone.message_type"));
        Assert.Equal("sales", GetTag(activity, "bondstone.source_module"));
        Assert.Equal("fulfillment", GetTag(activity, "bondstone.target_module"));
        Assert.Equal("reserve-order-handler", GetTag(activity, "bondstone.handler_identity"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenMessageTypeIsUnknown_ThrowsWithoutDelegating()
    {
        var registry = new MessageTypeRegistry();
        var inboxExecutor = new CapturingRebusInboxExecutor(DurableInboxHandleStatus.Handled);
        var pipeline = new RebusTypedCommandReceivePipeline(registry, inboxExecutor);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await pipeline.HandleOnceAsync<ReserveOrderCommand>(
                CreateEnvelope(),
                "reserve-order-handler",
                (_, _) => ValueTask.CompletedTask,
                _ => ValueTask.CompletedTask));

        Assert.Null(inboxExecutor.Envelope);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenRegisteredTypeIsEvent_ThrowsWithoutDelegating()
    {
        var registry = new MessageTypeRegistry();
        registry.Register<OrderReservedEvent>("fulfillment.order.reserve.v1");
        var inboxExecutor = new CapturingRebusInboxExecutor(DurableInboxHandleStatus.Handled);
        var pipeline = new RebusTypedCommandReceivePipeline(registry, inboxExecutor);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pipeline.HandleOnceAsync<ReserveOrderCommand>(
                CreateEnvelope(),
                "reserve-order-handler",
                (_, _) => ValueTask.CompletedTask,
                _ => ValueTask.CompletedTask));

        Assert.Null(inboxExecutor.Envelope);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenHandlerCommandTypeDoesNotMatchRegistration_Throws()
    {
        var registry = new MessageTypeRegistry();
        registry.Register<ReserveOrderCommand>("fulfillment.order.reserve.v1");
        var inboxExecutor = new CapturingRebusInboxExecutor(DurableInboxHandleStatus.Handled);
        var pipeline = new RebusTypedCommandReceivePipeline(registry, inboxExecutor);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pipeline.HandleOnceAsync<CancelOrderCommand>(
                CreateEnvelope(),
                "cancel-order-handler",
                (_, _) => ValueTask.CompletedTask,
                _ => ValueTask.CompletedTask));

        Assert.Null(inboxExecutor.Envelope);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenPayloadIsInvalid_ThrowsJsonException()
    {
        var registry = new MessageTypeRegistry();
        registry.Register<ReserveOrderCommand>("fulfillment.order.reserve.v1");
        var inboxExecutor = new CapturingRebusInboxExecutor(DurableInboxHandleStatus.Handled);
        var pipeline = new RebusTypedCommandReceivePipeline(registry, inboxExecutor);

        await Assert.ThrowsAsync<JsonException>(
            async () => await pipeline.HandleOnceAsync<ReserveOrderCommand>(
                CreateEnvelope(payload: "{"),
                "reserve-order-handler",
                (_, _) => ValueTask.CompletedTask,
                _ => ValueTask.CompletedTask));

        Assert.Null(inboxExecutor.Envelope);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneRebusTypedCommandReceivePipeline_RegistersTypedReceivePipeline()
    {
        var registry = new MessageTypeRegistry();
        var services = new ServiceCollection();
        services.AddSingleton<IMessageTypeRegistry>(registry);
        services.AddSingleton<IRebusDurableInboxHandlerExecutor>(
            new CapturingRebusInboxExecutor(DurableInboxHandleStatus.Handled));

        services.AddBondstoneRebusTypedCommandReceivePipeline();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Assert.IsType<MessageTypeRegistry>(
            serviceProvider.GetRequiredService<IMessageTypeRegistry>());
        Assert.IsType<RebusTypedCommandReceivePipeline>(
            serviceProvider.GetRequiredService<IRebusTypedCommandReceivePipeline>());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusTypedCommandReceivePipeline_WhenUsedInBondstoneBuilder_RegistersTypedPipeline()
    {
        var services = new ServiceCollection();

        services.AddBondstone(builder => builder.UseRebusTypedCommandReceivePipeline());

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusTypedCommandReceivePipeline)
                && descriptor.ImplementationType == typeof(RebusTypedCommandReceivePipeline));
    }

    private static ActivityListener Listen(List<Activity> stoppedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == BondstoneRebusTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                lock (stoppedActivities)
                {
                    stoppedActivities.Add(activity);
                }
            },
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static Activity[] Snapshot(List<Activity> activities)
    {
        lock (activities)
        {
            return activities.ToArray();
        }
    }

    private static string? GetTag(Activity activity, string key)
    {
        return activity.Tags.SingleOrDefault(tag => tag.Key == key).Value;
    }

    private static RebusDurableMessageEnvelope CreateEnvelope(
        string payload = """{"orderId":"A-100"}""")
    {
        return new RebusDurableMessageEnvelope(
            Guid.Parse("e37baceb-4d6f-4c91-9870-6d209cd258a8"),
            MessageKind.Command.ToString(),
            "fulfillment.order.reserve.v1",
            "sales",
            "fulfillment",
            payload,
            null,
            DateTimeOffset.Parse("2026-06-05T12:00:00+00:00"),
            DurableOperationId: null,
            TraceParent: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
            TraceState: null,
            TraceBaggage: null,
            CausationId: null,
            PartitionKey: "orders/A-100");
    }

    public sealed record ReserveOrderCommand(string OrderId) : IDurableCommand;

    public sealed record CancelOrderCommand(string OrderId) : IDurableCommand;

    public sealed record OrderReservedEvent(string OrderId) : IIntegrationEvent;

    private sealed class CapturingRebusInboxExecutor(DurableInboxHandleStatus status)
        : IRebusDurableInboxHandlerExecutor
    {
        public RebusDurableMessageEnvelope? Envelope { get; private set; }

        public string? HandlerIdentity { get; private set; }

        public int HandlerCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            RebusDurableMessageEnvelope envelope,
            string handlerIdentity,
            Func<DurableMessageEnvelope, CancellationToken, ValueTask> handler,
            Func<CancellationToken, ValueTask> commit,
            CancellationToken ct = default)
        {
            Envelope = envelope;
            HandlerIdentity = handlerIdentity;

            var record = new DurableInboxRecord(
                new DurableInboxMessageKey(
                    envelope.MessageId,
                    envelope.TargetModule!,
                    handlerIdentity),
                DateTimeOffset.Parse("2026-06-05T12:01:00+00:00"));

            if (status == DurableInboxHandleStatus.Handled)
            {
                HandlerCalls++;
                await handler(CreateDurableEnvelope(envelope), ct);
                await commit(ct);
            }

            return new DurableInboxHandleResult(status, record);
        }

        private static DurableMessageEnvelope CreateDurableEnvelope(
            RebusDurableMessageEnvelope envelope)
        {
            return new DurableMessageEnvelope(
                envelope.MessageId,
                MessageKind.Command,
                envelope.MessageTypeName,
                envelope.SourceModule,
                envelope.TargetModule,
                envelope.Payload,
                envelope.CreatedAtUtc,
                durableOperationId: envelope.DurableOperationId,
                traceContext: null,
                causationId: envelope.CausationId,
                partitionKey: envelope.PartitionKey,
                metadata: envelope.Metadata);
        }
    }
}
