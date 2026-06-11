using Bondstone.Modules;

namespace Bondstone.EntityFrameworkCore.Persistence;

internal sealed class EntityFrameworkCoreModuleRuntimeDescriptor(
    BondstoneModuleRegistration module,
    bool usesDomainEventPersistence)
{
    public BondstoneModuleRegistration Module { get; } =
        module ?? throw new ArgumentNullException(nameof(module));

    public bool UsesEntityFrameworkCorePersistence =>
        Module.UsesPersistence
        && StringComparer.Ordinal.Equals(
            Module.PersistenceProviderName,
            EntityFrameworkCoreModulePersistence.ProviderName);

    public bool UsesDomainEventPersistence { get; } = usesDomainEventPersistence;
}
