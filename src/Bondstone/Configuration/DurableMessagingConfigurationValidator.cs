using Bondstone.Modules;

namespace Bondstone.Configuration;

internal sealed class DurableMessagingConfigurationValidator
    : IBondstoneConfigurationValidator
{
    public void Validate(BondstoneConfigurationValidationContext context)
    {
        foreach (BondstoneModuleRegistration module in context.ModulesByName.Values
            .Where(static module => module.UsesDurableMessaging && !module.UsesPersistence))
        {
            throw new InvalidOperationException(
                $"Module '{module.Name}' uses durable messaging but does not declare persistence. Configure module persistence with UsePersistence or a provider-specific persistence opt-in such as UseEntityFrameworkCorePersistence<TDbContext>().");
        }

        foreach (ModuleCommandRoute route in context.DurableCommandRoutes)
        {
            if (!context.ModulesByName.TryGetValue(route.ModuleName, out BondstoneModuleRegistration? module))
            {
                throw new InvalidOperationException(
                    $"Module '{route.ModuleName}' registers durable command handler '{route.HandlerType.FullName}' for message type '{route.MessageTypeName}', but the module is not registered.");
            }

            if (!module.UsesDurableMessaging)
            {
                throw new InvalidOperationException(
                    $"Module '{route.ModuleName}' registers durable command handler '{route.HandlerType.FullName}' for message type '{route.MessageTypeName}', but the module does not use durable messaging. Call UseDurableMessaging for module '{route.ModuleName}'.");
            }
        }

        foreach (ModuleEventSubscriberRegistration subscriber in context.EventSubscribers)
        {
            if (!context.ModulesByName.TryGetValue(subscriber.ModuleName, out BondstoneModuleRegistration? module))
            {
                throw new InvalidOperationException(
                    $"Module '{subscriber.ModuleName}' registers durable event subscriber '{subscriber.HandlerType.FullName}' for message type '{subscriber.MessageTypeName}' and subscriber identity '{subscriber.SubscriberIdentity}', but the module is not registered.");
            }

            if (!module.UsesDurableMessaging)
            {
                throw new InvalidOperationException(
                    $"Module '{subscriber.ModuleName}' registers durable event subscriber '{subscriber.HandlerType.FullName}' for message type '{subscriber.MessageTypeName}' and subscriber identity '{subscriber.SubscriberIdentity}', but the module does not use durable messaging. Call UseDurableMessaging for module '{subscriber.ModuleName}'.");
            }
        }
    }
}
