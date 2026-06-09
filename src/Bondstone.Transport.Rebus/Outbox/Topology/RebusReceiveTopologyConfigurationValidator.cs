using Bondstone.Configuration;
using Bondstone.Modules;
using Bondstone.Transport.Rebus.Inbox;

namespace Bondstone.Transport.Rebus.Outbox;

internal sealed class RebusReceiveTopologyConfigurationValidator(
    IReadOnlyCollection<RebusModuleReceiveEndpointBinding> receiveEndpointBindings,
    IReadOnlyCollection<RebusEventSubscriptionBinding> eventSubscriptionBindings)
    : IBondstoneConfigurationValidator
{
    public RebusReceiveTopologyConfigurationValidator(
        IReadOnlyCollection<RebusModuleReceiveEndpointBinding> receiveEndpointBindings)
        : this(
            receiveEndpointBindings,
            [])
    {
    }

    public void Validate(BondstoneConfigurationValidationContext context)
    {
        foreach (RebusModuleReceiveEndpointBinding endpoint in receiveEndpointBindings)
        {
            foreach (string moduleName in endpoint.ModuleNames)
            {
                if (!context.ModulesByName.TryGetValue(
                    moduleName,
                    out BondstoneModuleRegistration? module))
                {
                    throw new InvalidOperationException(
                        $"Rebus receive endpoint '{endpoint.EndpointName}' accepts module '{moduleName}', but that module is not registered. Register module '{moduleName}' in AddBondstone or remove it from the Rebus receive endpoint.");
                }

                if (!module.UsesDurableMessaging)
                {
                    throw new InvalidOperationException(
                        $"Rebus receive endpoint '{endpoint.EndpointName}' accepts module '{moduleName}', but that module does not use durable messaging. Call UseDurableMessaging for module '{moduleName}'.");
                }

                if (!context.ModuleHasDurableCommandHandlers(moduleName))
                {
                    throw new InvalidOperationException(
                        $"Rebus receive endpoint '{endpoint.EndpointName}' accepts module '{moduleName}', but the module has no durable command handlers. Register at least one IDurableCommand handler for module '{moduleName}' or remove it from the Rebus receive endpoint.");
                }
            }
        }

        foreach (RebusEventSubscriptionBinding subscription in eventSubscriptionBindings)
        {
            if (!context.ModulesByName.TryGetValue(
                subscription.SubscriberModule,
                out BondstoneModuleRegistration? module))
            {
                throw new InvalidOperationException(
                    $"Rebus receive endpoint '{subscription.EndpointName}' subscribes module '{subscription.SubscriberModule}' to event '{subscription.MessageTypeName}', but that module is not registered. Register module '{subscription.SubscriberModule}' in AddBondstone or remove it from the Rebus event subscription.");
            }

            if (!module.UsesDurableMessaging)
            {
                throw new InvalidOperationException(
                    $"Rebus receive endpoint '{subscription.EndpointName}' subscribes module '{subscription.SubscriberModule}' to event '{subscription.MessageTypeName}', but that module does not use durable messaging. Call UseDurableMessaging for module '{subscription.SubscriberModule}'.");
            }

            bool subscriberExists = context.EventSubscribers.Any(subscriber =>
                StringComparer.Ordinal.Equals(subscriber.ModuleName, subscription.SubscriberModule)
                && StringComparer.Ordinal.Equals(subscriber.MessageTypeName, subscription.MessageTypeName)
                && StringComparer.Ordinal.Equals(subscriber.SubscriberIdentity, subscription.SubscriberIdentity));

            if (!subscriberExists)
            {
                throw new InvalidOperationException(
                    $"Rebus receive endpoint '{subscription.EndpointName}' subscribes module '{subscription.SubscriberModule}' to event '{subscription.MessageTypeName}' with subscriber identity '{subscription.SubscriberIdentity}', but no matching event subscriber is registered.");
            }
        }
    }
}
