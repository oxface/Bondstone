using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Modules;

public sealed class ModuleEventRegistrationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterPublishedEvent_WhenCalled_RegistersEventIdentity()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Events.RegisterPublishedEvent<CustomerRegisteredEvent>();
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMessageTypeRegistry messageTypeRegistry =
            serviceProvider.GetRequiredService<IMessageTypeRegistry>();

        MessageTypeRegistration registration = Assert.Single(messageTypeRegistry.Registrations);
        Assert.Equal(typeof(CustomerRegisteredEvent), registration.ClrType);
        Assert.Equal("sales.customer.registered.v1", registration.MessageTypeName);
        Assert.Equal(MessageKind.Event, registration.Kind);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterSubscriber_WhenCalled_RecordsStableSubscriberMetadata()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<CustomerRegisteredEvent, CustomerRegisteredHandler>(
                    " fulfillment.customer-cache.v1 ");
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IModuleEventSubscriberRegistry registry =
            serviceProvider.GetRequiredService<IModuleEventSubscriberRegistry>();

        ModuleEventSubscriberRegistration subscriber = Assert.Single(registry.Subscribers);
        Assert.Equal("fulfillment", subscriber.ModuleName);
        Assert.Equal(typeof(CustomerRegisteredEvent), subscriber.EventType);
        Assert.Equal(typeof(CustomerRegisteredHandler), subscriber.HandlerType);
        Assert.Equal("sales.customer.registered.v1", subscriber.MessageTypeName);
        Assert.Equal("fulfillment.customer-cache.v1", subscriber.SubscriberIdentity);
        Assert.Same(
            subscriber,
            Assert.Single(registry.GetByMessageTypeName(" sales.customer.registered.v1 ")));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterSubscriber_WithExplicitEventIdentity_RegistersMessageIdentity()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderSubmittedHandler>(
                    "sales.order.completed.v2",
                    "fulfillment.order-projector.v1");
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IModuleEventSubscriberRegistry registry =
            serviceProvider.GetRequiredService<IModuleEventSubscriberRegistry>();

        ModuleEventSubscriberRegistration subscriber = Assert.Single(registry.Subscribers);
        Assert.Equal("sales.order.completed.v2", subscriber.MessageTypeName);
        Assert.Equal(MessageKind.Event, subscriber.MessageTypeRegistration.Kind);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterSubscriber_WhenSubscriberIdentityIsEmpty_Throws()
    {
        var services = new ServiceCollection();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.Events.RegisterSubscriber<CustomerRegisteredEvent, CustomerRegisteredHandler>(" ");
                });
            }));

        Assert.Equal("subscriberIdentity", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterSubscriber_WhenSameSubscriberAlreadyExistsWithDifferentHandler_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<CustomerRegisteredEvent, CustomerRegisteredHandler>(
                    "fulfillment.customer-cache.v1");
                module.Events.RegisterSubscriber<CustomerRegisteredEvent, AlternateCustomerRegisteredHandler>(
                        "fulfillment.customer-cache.v1");
                });
            }));

        Assert.Contains("already has an event subscriber", exception.Message, StringComparison.Ordinal);
    }

    private static void ConfigureDurableMessaging(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.UsePersistence("test persistence");
    }

    [IntegrationEventIdentity("sales.customer.registered.v1")]
    public sealed record CustomerRegisteredEvent(string CustomerId) : IIntegrationEvent;

    public sealed class CustomerRegisteredHandler : IIntegrationEventHandler<CustomerRegisteredEvent>
    {
        public ValueTask HandleAsync(
            CustomerRegisteredEvent integrationEvent,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    public sealed class AlternateCustomerRegisteredHandler : IIntegrationEventHandler<CustomerRegisteredEvent>
    {
        public ValueTask HandleAsync(
            CustomerRegisteredEvent integrationEvent,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    public sealed record OrderSubmittedEvent(string OrderId) : IIntegrationEvent;

    public sealed class OrderSubmittedHandler : IIntegrationEventHandler<OrderSubmittedEvent>
    {
        public ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
