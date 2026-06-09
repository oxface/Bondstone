using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Inbox;

public sealed class RebusModuleEventReceivePipelineTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenSubscriberExists_ExecutesModuleEventSubscriberThroughInbox()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var services = new ServiceCollection();
        services.AddSingleton<EventCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);
        services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-09T12:01:00+00:00")));

        services.AddBondstone(bondstone =>
        {
            bondstone.UseRebusModuleEventReceiveTopology(
                [
                    new RebusEventSubscriptionBinding(
                        "fulfillment-events",
                        "sales.order.submitted.v1",
                        "fulfillment",
                        "fulfillment.order-projection.v1"),
                ]);
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
        IRebusModuleEventReceivePipeline pipeline =
            scope.ServiceProvider.GetRequiredService<IRebusModuleEventReceivePipeline>();

        DurableInboxHandleResult result = await pipeline.HandleOnceAsync(
            CreateEnvelope(),
            "fulfillment",
            "fulfillment.order-projection.v1");

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.NotNull(inboxExecutor.Record);
        Assert.Equal(CreateEnvelope().MessageId, inboxExecutor.Record.Key.MessageId);
        Assert.Equal("fulfillment", inboxExecutor.Record.Key.ModuleName);
        Assert.Equal("fulfillment.order-projection.v1", inboxExecutor.Record.Key.HandlerIdentity);
        Assert.Equal(DateTimeOffset.Parse("2026-06-09T12:01:00+00:00"), inboxExecutor.Record.ReceivedAtUtc);
        Assert.Equal(1, inboxExecutor.HandlerCalls);

        EventCallLog log = serviceProvider.GetRequiredService<EventCallLog>();
        Assert.Equal(["handle:A-100"], log.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenInboxRecordAlreadyReceived_ThrowsAndSkipsHandler()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.AlreadyReceived);
        var services = new ServiceCollection();
        services.AddSingleton<EventCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);

        services.AddBondstone(bondstone =>
        {
            bondstone.UseRebusModuleEventReceiveTopology(
                [
                    new RebusEventSubscriptionBinding(
                        "fulfillment-events",
                        "sales.order.submitted.v1",
                        "fulfillment",
                        "fulfillment.order-projection.v1"),
                ]);
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
        IRebusModuleEventReceivePipeline pipeline =
            scope.ServiceProvider.GetRequiredService<IRebusModuleEventReceivePipeline>();

        await Assert.ThrowsAsync<RebusDurableInboxAlreadyReceivedException>(
            async () => await pipeline.HandleOnceAsync(
                CreateEnvelope(),
                "fulfillment",
                "fulfillment.order-projection.v1"));

        Assert.Equal(0, inboxExecutor.HandlerCalls);
        Assert.Empty(serviceProvider.GetRequiredService<EventCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenEndpointHasTwoSubscribers_ExecutesBothSubscribers()
    {
        var result = CreateResult();
        var pipeline = new CapturingEventReceivePipeline(result);
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            pipeline,
            new RebusEventSubscriptionBinding(
                "fulfillment-events",
                "sales.order.submitted.v1",
                "fulfillment",
                "fulfillment.order-projection.v1"),
            new RebusEventSubscriptionBinding(
                "fulfillment-events",
                "sales.order.submitted.v1",
                "billing",
                "billing.order-audit.v1"));
        IRebusModuleEventEndpointDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRebusModuleEventEndpointDispatcher>();

        IReadOnlyCollection<DurableInboxHandleResult> results = await dispatcher.DispatchAsync(
            "fulfillment-events",
            CreateEnvelope());

        Assert.Equal(2, results.Count);
        Assert.Equal(2, pipeline.Calls.Count);
        Assert.Contains(
            pipeline.Calls,
            call => call.SubscriberModule == "fulfillment"
                && call.SubscriberIdentity == "fulfillment.order-projection.v1");
        Assert.Contains(
            pipeline.Calls,
            call => call.SubscriberModule == "billing"
                && call.SubscriberIdentity == "billing.order-audit.v1");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenEndpointHasNoSubscriptionForEvent_ThrowsBeforePipeline()
    {
        var pipeline = new CapturingEventReceivePipeline(CreateResult());
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            pipeline,
            new RebusEventSubscriptionBinding(
                "fulfillment-events",
                "other.event.v1",
                "fulfillment",
                "fulfillment.order-projection.v1"));
        IRebusModuleEventEndpointDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRebusModuleEventEndpointDispatcher>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync(
                "fulfillment-events",
                CreateEnvelope()));

        Assert.Contains("fulfillment-events", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales.order.submitted.v1", exception.Message, StringComparison.Ordinal);
        Assert.Empty(pipeline.Calls);
    }

    private static ServiceProvider CreateServiceProvider(
        CapturingEventReceivePipeline pipeline,
        params RebusEventSubscriptionBinding[] subscriptions)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IRebusModuleEventReceivePipeline>(pipeline);
        services.AddBondstoneRebusModuleEventReceiveTopology(subscriptions);

        return services.BuildServiceProvider();
    }

    private static DurableInboxHandleResult CreateResult()
    {
        var record = new DurableInboxRecord(
            DurableInboxMessageKey.ForEventSubscriber(
                Guid.Parse("8fb9313b-356d-4928-89ea-81b2e6261d27"),
                "fulfillment",
                "fulfillment.order-projection.v1"),
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"));

        return new DurableInboxHandleResult(DurableInboxHandleStatus.Handled, record);
    }

    private static RebusDurableMessageEnvelope CreateEnvelope()
    {
        return new RebusDurableMessageEnvelope(
            Guid.Parse("8fb9313b-356d-4928-89ea-81b2e6261d27"),
            MessageKind.Event.ToString(),
            "sales.order.submitted.v1",
            "sales",
            null,
            """{"orderId":"A-100"}""",
            null,
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"),
            DurableOperationId: null,
            TraceParent: null,
            TraceState: null,
            TraceBaggage: null,
            CausationId: null,
            PartitionKey: "orders/A-100");
    }

    [IntegrationEventIdentity("sales.order.submitted.v1")]
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

    private sealed class CapturingEventReceivePipeline(DurableInboxHandleResult result)
        : IRebusModuleEventReceivePipeline
    {
        public List<EventReceiveCall> Calls { get; } = [];

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            RebusDurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            Calls.Add(new EventReceiveCall(
                envelope,
                subscriberModule,
                subscriberIdentity,
                ct));

            return ValueTask.FromResult(result);
        }
    }

    private sealed record EventReceiveCall(
        RebusDurableMessageEnvelope Envelope,
        string SubscriberModule,
        string SubscriberIdentity,
        CancellationToken CancellationToken);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
