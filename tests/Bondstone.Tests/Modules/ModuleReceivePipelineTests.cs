using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Tests.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Modules;

public sealed class ModuleReceivePipelineTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task CommandReceive_WhenCommandRouteExists_ExecutesModuleCommandThroughInbox()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var services = CreateServices(inboxExecutor);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IModuleCommandReceivePipeline pipeline =
            scope.ServiceProvider.GetRequiredService<IModuleCommandReceivePipeline>();

        DurableInboxHandleResult result = await pipeline.HandleOnceAsync(CreateCommandEnvelope());

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.NotNull(inboxExecutor.Record);
        Assert.Equal(CreateCommandEnvelope().MessageId, inboxExecutor.Record.Key.MessageId);
        Assert.Equal("fulfillment", inboxExecutor.Record.Key.ModuleName);
        Assert.Equal("receive.fulfillment.order.reserve.v1", inboxExecutor.Record.Key.HandlerIdentity);
        Assert.Equal(DateTimeOffset.Parse("2026-06-06T12:01:00+00:00"), inboxExecutor.Record.ReceivedAtUtc);
        Assert.Equal(1, inboxExecutor.HandlerCalls);
        Assert.Equal(
            ["handle:A-100"],
            serviceProvider.GetRequiredService<CommandCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CommandReceive_UsesConfiguredDurablePayloadJsonOptions()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var services = CreateServices(inboxExecutor);
        services.ConfigureBondstoneDurablePayloadJson(
            options => options.Converters.Add(new DurableOrderIdJsonConverter()));

        services.AddBondstone(bondstone =>
        {
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
        IModuleCommandReceivePipeline pipeline =
            scope.ServiceProvider.GetRequiredService<IModuleCommandReceivePipeline>();

        DurableInboxHandleResult result = await pipeline.HandleOnceAsync(
            CreateConvertedCommandEnvelope());

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.Equal(
            ["handle-converted:A-100"],
            serviceProvider.GetRequiredService<CommandCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CommandReceive_WhenInboxRecordAlreadyReceived_ThrowsAndSkipsHandler()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.AlreadyReceived);
        var services = CreateServices(inboxExecutor);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IModuleCommandReceivePipeline pipeline =
            scope.ServiceProvider.GetRequiredService<IModuleCommandReceivePipeline>();

        await Assert.ThrowsAsync<DurableInboxAlreadyReceivedException>(
            async () => await pipeline.HandleOnceAsync(CreateCommandEnvelope()));

        Assert.Equal(0, inboxExecutor.HandlerCalls);
        Assert.Empty(serviceProvider.GetRequiredService<CommandCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EventReceive_WhenSubscriberExists_ExecutesModuleEventSubscriberThroughInbox()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var services = CreateServices(inboxExecutor);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderSubmittedHandler>(
                    "fulfillment.order-projection.v1");
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IModuleEventReceivePipeline pipeline =
            scope.ServiceProvider.GetRequiredService<IModuleEventReceivePipeline>();

        DurableInboxHandleResult result = await pipeline.HandleOnceAsync(
            CreateEventEnvelope(),
            "fulfillment",
            "fulfillment.order-projection.v1");

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.NotNull(inboxExecutor.Record);
        Assert.Equal(CreateEventEnvelope().MessageId, inboxExecutor.Record.Key.MessageId);
        Assert.Equal("fulfillment", inboxExecutor.Record.Key.ModuleName);
        Assert.Equal("fulfillment.order-projection.v1", inboxExecutor.Record.Key.HandlerIdentity);
        Assert.Equal(DateTimeOffset.Parse("2026-06-06T12:01:00+00:00"), inboxExecutor.Record.ReceivedAtUtc);
        Assert.Equal(1, inboxExecutor.HandlerCalls);
        Assert.Equal(
            ["handle:A-100"],
            serviceProvider.GetRequiredService<EventCallLog>().Calls);
    }

    private static ServiceCollection CreateServices(
        CapturingInboxHandlerExecutor inboxExecutor)
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddSingleton<EventCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);
        services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-06T12:01:00+00:00")));
        return services;
    }

    private static DurableMessageEnvelope CreateCommandEnvelope(
        string payload = """{"orderId":"A-100"}""")
    {
        return new DurableMessageEnvelope(
            Guid.Parse("e37baceb-4d6f-4c91-9870-6d209cd258a8"),
            MessageKind.Command,
            "receive.fulfillment.order.reserve.v1",
            "sales",
            "fulfillment",
            payload,
            DateTimeOffset.Parse("2026-06-05T12:00:00+00:00"),
            traceContext: new MessageTraceContext(
                "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"),
            partitionKey: "orders/A-100");
    }

    private static DurableMessageEnvelope CreateConvertedCommandEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("e37baceb-4d6f-4c91-9870-6d209cd258a8"),
            MessageKind.Command,
            "receive.fulfillment.order.reserve-converted.v1",
            "sales",
            "fulfillment",
            """{"orderId":"payload-A-100"}""",
            DateTimeOffset.Parse("2026-06-05T12:00:00+00:00"),
            partitionKey: "orders/A-100");
    }

    private static DurableMessageEnvelope CreateEventEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("8fb9313b-356d-4928-89ea-81b2e6261d27"),
            MessageKind.Event,
            "receive.sales.order.submitted.v1",
            "sales",
            targetModule: null,
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-05T12:00:00+00:00"),
            partitionKey: "orders/A-100");
    }

    [DurableCommandIdentity("receive.fulfillment.order.reserve.v1")]
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

    [DurableCommandIdentity("receive.fulfillment.order.reserve-converted.v1")]
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

    [IntegrationEventIdentity("receive.sales.order.submitted.v1")]
    public sealed record OrderSubmittedEvent(string OrderId) : IIntegrationEvent;

    public sealed class OrderSubmittedHandler(EventCallLog log)
        : IIntegrationEventHandler<OrderSubmittedEvent>
    {
        public ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle:{integrationEvent.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class CommandCallLog
    {
        public List<string> Calls { get; } = [];
    }

    public sealed class EventCallLog
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
