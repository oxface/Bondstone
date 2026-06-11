using Bondstone.Modules;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Utility;

namespace Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventModuleRuntimeRegistry(
    IBondstoneModuleRegistry moduleRegistry,
    IEnumerable<EntityFrameworkCoreDomainEventPersistenceModule>
        domainEventPersistenceModules)
{
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
    private readonly Lazy<IReadOnlySet<string>>
        _domainEventPersistenceModules =
            new(() => BuildDomainEventPersistenceModuleSet(domainEventPersistenceModules));

    public EntityFrameworkCoreDomainEventModuleRuntimeDescriptor GetRuntime(string moduleName)
    {
        BondstoneModuleRegistration module = _moduleRegistry.GetModule(moduleName);
        bool usesDomainEventPersistence = _domainEventPersistenceModules.Value.Contains(module.Name);

        return new EntityFrameworkCoreDomainEventModuleRuntimeDescriptor(
            module,
            usesDomainEventPersistence);
    }

    private static IReadOnlySet<string> BuildDomainEventPersistenceModuleSet(
            IEnumerable<EntityFrameworkCoreDomainEventPersistenceModule> modules)
    {
        HashSet<string> moduleNames = new(StringComparer.Ordinal);

        foreach (EntityFrameworkCoreDomainEventPersistenceModule module in modules)
        {
            string moduleName = module.ModuleName.NormalizeRequired(
                nameof(EntityFrameworkCoreDomainEventPersistenceModule.ModuleName),
                "Module name");

            moduleNames.Add(moduleName);
        }

        return moduleNames;
    }
}

internal sealed class EntityFrameworkCoreDomainEventModuleRuntimeDescriptor(
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
