using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Transport.RabbitMq.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.RabbitMq.Tests;

public sealed class RabbitMqTopologyConfigurationValidatorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDurableCommandHandlerHasNoRabbitMqRoute_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                    module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
                });
                bondstone.UseRabbitMqTransport(
                    rabbitMq => rabbitMq.UseCommandExchange("bondstone.commands"));
            }));

        Assert.Contains("No durable envelope dispatch route", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            "No RabbitMQ routing key is configured for target module 'fulfillment'.",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenPublishedEventHasNoRabbitMqRoute_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("sales", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                    module.Events.RegisterPublishedEvent<TestEvent>();
                });
                bondstone.UseRabbitMqTransport(
                    rabbitMq => rabbitMq.UseEventExchange("bondstone.events"));
            }));

        Assert.Contains("No durable envelope dispatch route", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales.test.event.v1", exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            "No RabbitMQ routing key is configured for message type 'sales.test.event.v1'.",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenReceiveQueueAcceptsModuleWithoutHandlers_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                });
                bondstone.UseRabbitMqTransport(
                    rabbitMq => rabbitMq.ReceiveQueue("fulfillment.commands")
                        .AcceptModule("fulfillment"));
            }));

        Assert.Contains("has no registered durable command handlers", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment.commands", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenOutboxTransportOverloadHasInvalidReceiveBinding_SkipsProviderValidation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
            });
            bondstone.Outbox.UseRabbitMqTransport(
                rabbitMq => rabbitMq.ReceiveQueue("fulfillment.commands")
                    .AcceptModule("fulfillment"));
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetRequiredService<IRabbitMqTopologyDiagnostics>());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenReceiveQueueNamesUnregisteredSubscriber_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
                bondstone.UseRabbitMqTransport(
                    rabbitMq => rabbitMq.ReceiveQueue("sales-events")
                        .SubscribeEvent(
                            "sales.test.event.v1",
                            "fulfillment",
                            "fulfillment.test-event.v1"))));

        Assert.Contains("no matching Bondstone event subscriber", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment.test-event.v1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenRegisteredSubscriberHasNoRabbitMqReceiveBinding_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                    module.Events.RegisterSubscriber<TestEvent, TestEventHandler>(
                        "fulfillment.test-event.v1");
                });
                bondstone.UseRabbitMqTransport(_ => { });
            }));

        Assert.Contains("has no receive binding", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment.test-event.v1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenRabbitMqQueueEventRouteHasSplitReceiveQueues_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                RegisterPublishedAndSubscribedEvent(bondstone);
                bondstone.UseRabbitMqTransport(rabbitMq =>
                {
                    rabbitMq.RouteEvent("sales.test.event.v1").ToQueue("sales-events");
                    rabbitMq.ReceiveQueue("fulfillment-events")
                        .SubscribeEvent(
                            "sales.test.event.v1",
                            "fulfillment",
                            "fulfillment.test-event.v1");
                    rabbitMq.ReceiveQueue("billing-events")
                        .SubscribeEvent(
                            "sales.test.event.v1",
                            "billing",
                            "billing.test-event.v1");
                });
            }));

        Assert.Contains("routed directly to queue 'sales-events'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment-events", exception.Message, StringComparison.Ordinal);
        Assert.Contains("billing-events", exception.Message, StringComparison.Ordinal);
        Assert.Contains("split subscribers should use an exchange route", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenRabbitMqQueueEventRouteUsesSameReceiveQueue_AllowsInProcessFanOut()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());

        services.AddBondstone(bondstone =>
        {
            RegisterPublishedAndSubscribedEvent(bondstone);
            bondstone.UseRabbitMqTransport(rabbitMq =>
            {
                rabbitMq.RouteEvent("sales.test.event.v1").ToQueue("sales-events");
                rabbitMq.ReceiveQueue("sales-events")
                    .SubscribeEvent(
                        "sales.test.event.v1",
                        "fulfillment",
                        "fulfillment.test-event.v1")
                    .SubscribeEvent(
                        "sales.test.event.v1",
                        "billing",
                        "billing.test-event.v1");
            });
        });
    }

    private static void RegisterPublishedAndSubscribedEvent(
        BondstoneBuilder bondstone)
    {
        bondstone.Module("sales", module =>
        {
            module.UseDurableMessaging();
            module.UsePersistence("test persistence");
            module.Events.RegisterPublishedEvent<TestEvent>();
        });
        bondstone.Module("fulfillment", module =>
        {
            module.UseDurableMessaging();
            module.UsePersistence("test persistence");
            module.Events.RegisterSubscriber<TestEvent, TestEventHandler>(
                "fulfillment.test-event.v1");
        });
        bondstone.Module("billing", module =>
        {
            module.UseDurableMessaging();
            module.UsePersistence("test persistence");
            module.Events.RegisterSubscriber<TestEvent, TestEventHandler>(
                "billing.test-event.v1");
        });
    }

    [DurableCommandIdentity("fulfillment.test.command.v1")]
    private sealed record TestCommand : IDurableCommand;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public ValueTask HandleAsync(
            TestCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [IntegrationEventIdentity("sales.test.event.v1")]
    private sealed record TestEvent : IIntegrationEvent;

    private sealed class TestEventHandler : IIntegrationEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(
            TestEvent integrationEvent,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingRabbitMqMessagePublisher : IRabbitMqMessagePublisher
    {
        public ValueTask PublishAsync(
            RabbitMqPublishDestination destination,
            RabbitMqTransportMessage message,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
