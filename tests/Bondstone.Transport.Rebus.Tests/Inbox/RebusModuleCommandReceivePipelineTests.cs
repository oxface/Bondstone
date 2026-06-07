using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Inbox;

public sealed class RebusModuleCommandReceivePipelineTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenCommandRouteExists_ExecutesModuleCommandThroughInbox()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);
        services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-06T12:01:00+00:00")));

        services.AddBondstone(bondstone =>
        {
            bondstone.UseRebusModuleCommandReceivePipeline();
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IRebusModuleCommandReceivePipeline pipeline =
            scope.ServiceProvider.GetRequiredService<IRebusModuleCommandReceivePipeline>();

        DurableInboxHandleResult result = await pipeline.HandleOnceAsync(CreateEnvelope());

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.NotNull(inboxExecutor.Record);
        Assert.Equal(CreateEnvelope().MessageId, inboxExecutor.Record.Key.MessageId);
        Assert.Equal("fulfillment", inboxExecutor.Record.Key.ModuleName);
        Assert.Equal("fulfillment.order.reserve.v1", inboxExecutor.Record.Key.HandlerIdentity);
        Assert.Equal(DateTimeOffset.Parse("2026-06-06T12:01:00+00:00"), inboxExecutor.Record.ReceivedAtUtc);
        Assert.Equal(1, inboxExecutor.HandlerCalls);

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(["handle:A-100"], log.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_UsesConfiguredDurablePayloadJsonOptions()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);
        services.ConfigureBondstoneDurablePayloadJson(
            options => options.Converters.Add(new DurableOrderIdJsonConverter()));

        services.AddBondstone(bondstone =>
        {
            bondstone.UseRebusModuleCommandReceivePipeline();
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<
                    ReserveConvertedOrderCommand,
                    ReserveConvertedOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IRebusModuleCommandReceivePipeline pipeline =
            scope.ServiceProvider.GetRequiredService<IRebusModuleCommandReceivePipeline>();

        DurableInboxHandleResult result = await pipeline.HandleOnceAsync(
            CreateConvertedEnvelope());

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(["handle-converted:A-100"], log.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenInboxRecordAlreadyReceived_ThrowsAndSkipsHandler()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.AlreadyReceived);
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);
        services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-06T12:01:00+00:00")));

        services.AddBondstone(bondstone =>
        {
            bondstone.UseRebusModuleCommandReceivePipeline();
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IRebusModuleCommandReceivePipeline pipeline =
            scope.ServiceProvider.GetRequiredService<IRebusModuleCommandReceivePipeline>();

        await Assert.ThrowsAsync<RebusDurableInboxAlreadyReceivedException>(
            async () => await pipeline.HandleOnceAsync(CreateEnvelope()));

        Assert.Equal(0, inboxExecutor.HandlerCalls);
        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Empty(log.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusModuleCommandReceivePipeline_WhenUsedInBondstoneBuilder_RegistersPipeline()
    {
        var services = new ServiceCollection();

        services.AddBondstone(builder => builder.UseRebusModuleCommandReceivePipeline());

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusModuleCommandReceivePipeline)
                && descriptor.ImplementationType == typeof(RebusModuleCommandReceivePipeline));
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

    private static RebusDurableMessageEnvelope CreateConvertedEnvelope()
    {
        return new RebusDurableMessageEnvelope(
            Guid.Parse("e37baceb-4d6f-4c91-9870-6d209cd258a8"),
            MessageKind.Command.ToString(),
            "fulfillment.order.reserve-converted.v1",
            "sales",
            "fulfillment",
            """{"orderId":"payload-A-100"}""",
            null,
            DateTimeOffset.Parse("2026-06-05T12:00:00+00:00"),
            DurableOperationId: null,
            TraceParent: null,
            TraceState: null,
            TraceBaggage: null,
            CausationId: null,
            PartitionKey: "orders/A-100");
    }

    [DurableCommandIdentity("fulfillment.order.reserve.v1")]
    public sealed record ReserveOrderCommand(string OrderId) : IDurableCommand;

    public sealed class ReserveOrderHandler(CommandCallLog log)
        : ICommandHandler<ReserveOrderCommand>
    {
        public ValueTask HandleAsync(
            ReserveOrderCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle:{command.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("fulfillment.order.reserve-converted.v1")]
    public sealed record ReserveConvertedOrderCommand(DurableOrderId OrderId)
        : IDurableCommand;

    public sealed class ReserveConvertedOrderHandler(CommandCallLog log)
        : ICommandHandler<ReserveConvertedOrderCommand>
    {
        public ValueTask HandleAsync(
            ReserveConvertedOrderCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle-converted:{command.OrderId.Value}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class CommandCallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class CapturingInboxHandlerExecutor(DurableInboxHandleStatus status)
        : IDurableInboxHandlerExecutor
    {
        public DurableInboxRecord? Record { get; private set; }

        public int HandlerCalls { get; private set; }

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
                await commit(ct);
            }

            return new DurableInboxHandleResult(status, record);
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
