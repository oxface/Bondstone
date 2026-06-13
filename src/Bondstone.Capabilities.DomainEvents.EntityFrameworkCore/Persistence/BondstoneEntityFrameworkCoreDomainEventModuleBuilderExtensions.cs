using Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.DomainEvents;
using Bondstone.Modules;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Persistence;

public static class BondstoneEntityFrameworkCoreDomainEventModuleBuilderExtensions
{
    public static BondstoneModuleBuilder UseEntityFrameworkCoreDomainEventPersistence(
        this BondstoneModuleBuilder module)
    {
        ArgumentNullException.ThrowIfNull(module);

        module.TryAddEntityFrameworkCoreDomainEventSystemBehaviors();

        return module;
    }

    private static void TryAddEntityFrameworkCoreDomainEventSystemBehaviors(
        this BondstoneModuleBuilder module)
    {
        module.Services.TryAddScoped(serviceProvider =>
            new EntityFrameworkCoreDomainEventModuleRuntimeRegistry(
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>()));
        module.AddCommandPipelineContribution(
            new ModuleCommandPipelineContribution(
                "Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Command",
                ModulePipelineStepKind.Capability,
                EntityFrameworkCoreDomainEventModulePipelineOrder.Command,
                typeof(EntityFrameworkCoreDomainEventModuleCommandBehavior<>),
                AppliesToModule));
        module.AddEventSubscriberPipelineContribution(
            new ModuleEventSubscriberPipelineContribution(
                "Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.EventSubscriber",
                ModulePipelineStepKind.Capability,
                EntityFrameworkCoreDomainEventModulePipelineOrder.EventSubscriber,
                typeof(EntityFrameworkCoreDomainEventModuleEventSubscriberBehavior<>),
                AppliesToModule));
    }

    private static bool AppliesToModule(BondstoneModuleRegistration module)
    {
        return module.UsesPersistence
            && StringComparer.Ordinal.Equals(
                module.PersistenceProviderName,
                EntityFrameworkCoreModulePersistence.ProviderName);
    }
}
