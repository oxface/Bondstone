using Bondstone.Modules;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Utility;

namespace Bondstone.Persistence.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventModuleOptInRegistry
{
    private readonly HashSet<string> _moduleNames = new(StringComparer.Ordinal);

    public void Enable(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        lock (_moduleNames)
        {
            _moduleNames.Add(normalizedModuleName);
        }
    }

    public bool IsEnabled(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        lock (_moduleNames)
        {
            return _moduleNames.Contains(normalizedModuleName);
        }
    }
}

internal sealed class EntityFrameworkCoreDomainEventModuleRuntimeRegistry(
    IBondstoneModuleRegistry moduleRegistry,
    EntityFrameworkCoreDomainEventModuleOptInRegistry optInRegistry)
{
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
    private readonly EntityFrameworkCoreDomainEventModuleOptInRegistry _optInRegistry =
        optInRegistry ?? throw new ArgumentNullException(nameof(optInRegistry));

    public EntityFrameworkCoreDomainEventModuleRuntimeDescriptor GetRuntime(string moduleName)
    {
        BondstoneModuleRegistration module = _moduleRegistry.GetModule(moduleName);

        return new EntityFrameworkCoreDomainEventModuleRuntimeDescriptor(
            module,
            _optInRegistry.IsEnabled(module.Name));
    }
}

internal sealed class EntityFrameworkCoreDomainEventModuleRuntimeDescriptor(
    BondstoneModuleRegistration module,
    bool usesDomainEventPersistence)
{
    public BondstoneModuleRegistration Module { get; } =
        module ?? throw new ArgumentNullException(nameof(module));

    public bool UsesDomainEventPersistence { get; } = usesDomainEventPersistence;

    public bool UsesEntityFrameworkCorePersistence =>
        UsesDomainEventPersistence
        &&
        Module.UsesPersistence
        && StringComparer.Ordinal.Equals(
            Module.PersistenceProviderName,
            EntityFrameworkCoreModulePersistence.ProviderName);
}
