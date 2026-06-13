using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Transport.ServiceBus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.ServiceBus.Tests;

public sealed class ServiceBusTopologyConfigurationValidatorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDurableCommandHandlerHasNoServiceBusDestination_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                    module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
                });
                bondstone.UseServiceBusTransport(_ => { });
            }));

        Assert.Contains("No durable outbox transport route", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            "No Service Bus queue is configured for target module 'fulfillment'.",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenPublishedEventHasNoServiceBusDestination_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("sales", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                    module.Events.RegisterPublishedEvent<TestEvent>();
                });
                bondstone.UseServiceBusTransport(_ => { });
            }));

        Assert.Contains("No durable outbox transport route", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales.test.event.v1", exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            "No Service Bus event destination is configured for message type 'sales.test.event.v1'.",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenReceiveSourceAcceptsModuleWithoutHandlers_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                });
                bondstone.UseServiceBusTransport(
                    serviceBus => serviceBus.ReceiveQueue("fulfillment-commands")
                        .AcceptModule("fulfillment"));
            }));

        Assert.Contains("has no registered durable command handlers", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment-commands", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenOutboxTransportOverloadHasInvalidReceiveBinding_SkipsProviderValidation()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
            });
            bondstone.Outbox.UseServiceBusTransport(
                serviceBus => serviceBus.ReceiveQueue("fulfillment-commands")
                    .AcceptModule("fulfillment"));
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetRequiredService<IServiceBusTopologyDiagnostics>());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenReceiveSourceNamesUnregisteredSubscriber_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
                bondstone.UseServiceBusTransport(
                    serviceBus => serviceBus.ReceiveSubscription("sales-events", "fulfillment")
                        .SubscribeEvent(
                            "sales.test.event.v1",
                            "fulfillment",
                            "fulfillment.test-event.v1"))));

        Assert.Contains("no matching Bondstone event subscriber", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment.test-event.v1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenRegisteredSubscriberHasNoServiceBusReceiveBinding_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());

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
                bondstone.UseServiceBusTransport(_ => { });
            }));

        Assert.Contains("has no receive binding", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment.test-event.v1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenServiceBusQueueEventDestinationHasSplitReceiveQueues_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                RegisterPublishedAndSubscribedEvent(bondstone);
                bondstone.UseServiceBusTransport(serviceBus =>
                {
                    serviceBus.RouteEvent("sales.test.event.v1").ToQueue("sales-events");
                    serviceBus.ReceiveQueue("fulfillment-events")
                        .SubscribeEvent(
                            "sales.test.event.v1",
                            "fulfillment",
                            "fulfillment.test-event.v1");
                    serviceBus.ReceiveQueue("billing-events")
                        .SubscribeEvent(
                            "sales.test.event.v1",
                            "billing",
                            "billing.test-event.v1");
                });
            }));

        Assert.Contains("routed directly to queue 'sales-events'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment-events", exception.Message, StringComparison.Ordinal);
        Assert.Contains("billing-events", exception.Message, StringComparison.Ordinal);
        Assert.Contains("split subscribers should use a topic", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenServiceBusQueueEventDestinationUsesSameReceiveQueue_AllowsInProcessFanOut()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());

        services.AddBondstone(bondstone =>
        {
            RegisterPublishedAndSubscribedEvent(bondstone);
            bondstone.UseServiceBusTransport(serviceBus =>
            {
                serviceBus.RouteEvent("sales.test.event.v1").ToQueue("sales-events");
                serviceBus.ReceiveQueue("sales-events")
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

    private sealed class RecordingServiceBusMessageSender : IServiceBusMessageSender
    {
        public ValueTask SendAsync(
            string entityName,
            ServiceBusTransportMessage message,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
