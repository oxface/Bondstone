using Bondstone.Modules;

namespace Bondstone.Persistence.Postgres.Persistence;

internal sealed class PostgresModuleRuntimeDescriptor(BondstoneModuleRegistration module)
{
    public BondstoneModuleRegistration Module { get; } =
        module ?? throw new ArgumentNullException(nameof(module));

    public bool UsesPostgresPersistence =>
        Module.UsesPersistence
        && StringComparer.Ordinal.Equals(
            Module.PersistenceProviderName,
            PostgresModulePersistence.ProviderName);
}
