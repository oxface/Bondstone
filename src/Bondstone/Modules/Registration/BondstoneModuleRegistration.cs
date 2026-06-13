using Bondstone.Utility;

namespace Bondstone.Modules;

public sealed class BondstoneModuleRegistration
{
    public BondstoneModuleRegistration(
        string name,
        bool usesDurableMessaging,
        string? persistenceProviderName = null,
        Type? persistenceContextType = null)
    {
        Name = name.NormalizeRequired(nameof(name), "Module name");
        UsesDurableMessaging = usesDurableMessaging;
        PersistenceProviderName = persistenceProviderName?.NormalizeRequired(
            nameof(persistenceProviderName),
            "Persistence provider name");
        PersistenceContextType = persistenceContextType;
    }

    public string Name { get; }

    public bool UsesDurableMessaging { get; }

    public bool UsesPersistence => PersistenceProviderName is not null;

    public string? PersistenceProviderName { get; }

    public Type? PersistenceContextType { get; }
}
