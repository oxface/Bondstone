using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Serialization.Json;
using Rebus.Transport.InMem;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Inbox;

public sealed class RebusModuleEventEndpointHandlerTransportTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Publish_WhenModuleEventEndpointHandlerIsSubscribed_DispatchesToModuleEventSubscriber()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var eventSignal = new ReceivedEventSignal();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            inboxExecutor,
            eventSignal);

        using var activator = new BuiltinHandlerActivator();
        activator.Handle<RebusDurableMessageEnvelope>(
            async envelope =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                RebusModuleCommandEndpointHandler handler =
                    scope.ServiceProvider.GetRequiredService<RebusModuleCommandEndpointHandler>();

                await handler.Handle(envelope);
            });

        var network = new InMemNetwork();
        using IBus bus = StartBus(activator, network, "fulfillment-events");

        await bus.Advanced.Topics.Subscribe("sales.order.submitted.v1");
        await bus.Advanced.Topics.Publish(
            "sales.order.submitted.v1",
            CreateEnvelope(),
            new Dictionary<string, string>());

        OrderSubmittedEvent handledEvent = await WaitAsync(eventSignal.Task);

        Assert.Equal("A-100", handledEvent.OrderId);
        Assert.NotNull(inboxExecutor.Record);
        Assert.Equal(CreateEnvelope().MessageId, inboxExecutor.Record.Key.MessageId);
        Assert.Equal("fulfillment", inboxExecutor.Record.Key.ModuleName);
        Assert.Equal("fulfillment.order-projection.v1", inboxExecutor.Record.Key.HandlerIdentity);
        Assert.Equal(1, inboxExecutor.HandlerCalls);
        Assert.Equal(1, inboxExecutor.CommitCalls);
        Assert.Equal(0, network.Count("fulfillment-events"));
        Assert.Equal(0, network.Count("error"));
    }

    private static ServiceProvider CreateServiceProvider(
        CapturingInboxHandlerExecutor inboxExecutor,
        ReceivedEventSignal eventSignal)
    {
        var services = new ServiceCollection();

        services.AddSingleton(eventSignal);
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);
        services.AddBondstoneRebusModuleCommandEndpointHandler("fulfillment-events");
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderSubmittedHandler>(
                    "fulfillment.order-projection.v1");
            });
            bondstone.UseRebusTransport(rebus =>
            {
                rebus.ReceiveEndpoint("fulfillment-events").SubscribeEvent(
                    "sales.order.submitted.v1",
                    "fulfillment",
                    "fulfillment.order-projection.v1");
            });
        });

        return services.BuildServiceProvider();
    }

    private static IBus StartBus(
        BuiltinHandlerActivator activator,
        InMemNetwork network,
        string inputQueueName)
    {
        return Configure
            .With(activator)
            .Transport(transport => transport.UseInMemoryTransport(network, inputQueueName))
            .Serialization(serializer => serializer.UseSystemTextJson())
            .Options(options => options.RetryStrategy("error", maxDeliveryAttempts: 1))
            .Start();
    }

    private static async Task<T> WaitAsync<T>(Task<T> task)
    {
        Task completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        if (completed != task)
        {
            throw new TimeoutException("Timed out waiting for Rebus module event endpoint handler signal.");
        }

        return await task;
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
            TraceParent: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
            TraceState: null,
            TraceBaggage: null,
            CausationId: null,
            PartitionKey: "orders/A-100");
    }

    [IntegrationEventIdentity("sales.order.submitted.v1")]
    public sealed record OrderSubmittedEvent(string OrderId) : IIntegrationEvent;

    public sealed class OrderSubmittedHandler(ReceivedEventSignal signal)
        : IIntegrationEventHandler<OrderSubmittedEvent>
    {
        public ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            CancellationToken ct = default)
        {
            signal.SetResult(integrationEvent);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ReceivedEventSignal
    {
        private readonly TaskCompletionSource<OrderSubmittedEvent> _source =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<OrderSubmittedEvent> Task => _source.Task;

        public void SetResult(OrderSubmittedEvent integrationEvent)
        {
            _source.TrySetResult(integrationEvent);
        }
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

            return new DurableInboxHandleResult(status, record);
        }
    }
}
