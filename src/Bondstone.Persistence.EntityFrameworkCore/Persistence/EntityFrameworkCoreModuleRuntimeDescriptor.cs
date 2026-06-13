using Bondstone.Modules;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

internal sealed class EntityFrameworkCoreModuleRuntimeDescriptor(
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
