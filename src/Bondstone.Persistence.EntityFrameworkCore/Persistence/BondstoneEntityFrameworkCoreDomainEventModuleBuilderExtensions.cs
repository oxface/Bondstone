using Bondstone.Persistence.EntityFrameworkCore.DomainEvents;
using Bondstone.Modules;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

/// <summary>
/// Adds EF Core persistence behavior for Bondstone module-local domain events.
/// </summary>
public static class BondstoneEntityFrameworkCoreDomainEventModuleBuilderExtensions
{
    /// <summary>
    /// Configures a module to collect and stage EF-backed module-local domain event records inside the module transaction.
    /// </summary>
    /// <param name="module">The module builder.</param>
    /// <returns>The same module builder for chained setup.</returns>
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
                "Bondstone.Persistence.EntityFrameworkCore.DomainEvents.Command",
                ModulePipelineStepKind.Capability,
                EntityFrameworkCoreDomainEventModulePipelineOrder.Command,
                typeof(EntityFrameworkCoreDomainEventModuleCommandBehavior<>),
                AppliesToModule));
        module.AddEventSubscriberPipelineContribution(
            new ModuleEventSubscriberPipelineContribution(
                "Bondstone.Persistence.EntityFrameworkCore.DomainEvents.EventSubscriber",
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
