using Bondstone.Modules;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;

namespace Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventModuleRuntimeRegistry(
    IBondstoneModuleRegistry moduleRegistry)
{
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));

    public EntityFrameworkCoreDomainEventModuleRuntimeDescriptor GetRuntime(string moduleName)
    {
        BondstoneModuleRegistration module = _moduleRegistry.GetModule(moduleName);

        return new EntityFrameworkCoreDomainEventModuleRuntimeDescriptor(module);
    }
}

internal sealed class EntityFrameworkCoreDomainEventModuleRuntimeDescriptor(
    BondstoneModuleRegistration module)
{
    public BondstoneModuleRegistration Module { get; } =
        module ?? throw new ArgumentNullException(nameof(module));

    public bool UsesEntityFrameworkCorePersistence =>
        Module.UsesPersistence
        && StringComparer.Ordinal.Equals(
            Module.PersistenceProviderName,
            EntityFrameworkCoreModulePersistence.ProviderName);
}
