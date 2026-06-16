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

        module.RequireEntityFrameworkCorePersistence();
        module.Services
            .GetOrAddEntityFrameworkCoreDomainEventModuleOptInRegistry()
            .Enable(module.Name);
        module.TryAddEntityFrameworkCoreDomainEventSystemBehaviors();

        return module;
    }

    private static void TryAddEntityFrameworkCoreDomainEventSystemBehaviors(
        this BondstoneModuleBuilder module)
    {
        module.Services.TryAddScoped(serviceProvider =>
            new EntityFrameworkCoreDomainEventModuleRuntimeRegistry(
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>(),
                serviceProvider.GetRequiredService<EntityFrameworkCoreDomainEventModuleOptInRegistry>()));
        module.Services.TryAddEnumerable(
            ServiceDescriptor.Scoped(
                typeof(IModulePostHandlerAction),
                typeof(EntityFrameworkCoreDomainEventModulePostHandlerAction)));
    }

    private static void RequireEntityFrameworkCorePersistence(
        this BondstoneModuleBuilder module)
    {
        ServiceDescriptor? descriptor = module.Services.FirstOrDefault(static descriptor =>
            descriptor.ServiceType == typeof(IBondstoneModuleRegistry));

        if (descriptor?.ImplementationInstance is not IBondstoneModuleRegistry moduleRegistry)
        {
            throw new InvalidOperationException(
                "Bondstone module registry was not available while configuring Entity Framework Core domain event persistence.");
        }

        BondstoneModuleRegistration registration = moduleRegistry.GetModule(module.Name);
        if (registration.UsesPersistence
            && StringComparer.Ordinal.Equals(
                registration.PersistenceProviderName,
                EntityFrameworkCoreModulePersistence.ProviderName))
        {
            return;
        }

        throw new InvalidOperationException(
            $"Module '{module.Name}' cannot use '{nameof(UseEntityFrameworkCoreDomainEventPersistence)}' before declaring "
            + $"persistence provider '{EntityFrameworkCoreModulePersistence.ProviderName}'. Configure the module with "
            + "UseEntityFrameworkCorePersistence<TDbContext>() first.");
    }

    private static EntityFrameworkCoreDomainEventModuleOptInRegistry
        GetOrAddEntityFrameworkCoreDomainEventModuleOptInRegistry(
            this IServiceCollection services)
    {
        ServiceDescriptor? descriptor = services.FirstOrDefault(static descriptor =>
            descriptor.ServiceType == typeof(EntityFrameworkCoreDomainEventModuleOptInRegistry));

        if (descriptor?.ImplementationInstance is EntityFrameworkCoreDomainEventModuleOptInRegistry existingRegistry)
        {
            return existingRegistry;
        }

        if (descriptor is not null)
        {
            throw new InvalidOperationException(
                "Entity Framework Core domain event module opt-in registry was already registered by a factory or implementation type.");
        }

        var registry = new EntityFrameworkCoreDomainEventModuleOptInRegistry();
        services.AddSingleton(registry);
        return registry;
    }
}
