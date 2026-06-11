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
    private readonly Lazy<IReadOnlySet<string>> _domainEventPersistenceModules =
        new(() => domainEventPersistenceModules
            .Select(static module => module.ModuleName.NormalizeRequired(
                nameof(EntityFrameworkCoreDomainEventPersistenceModule.ModuleName),
                "Module name"))
            .ToHashSet(StringComparer.Ordinal));

    public EntityFrameworkCoreDomainEventModuleRuntimeDescriptor GetRuntime(string moduleName)
    {
        BondstoneModuleRegistration module = _moduleRegistry.GetModule(moduleName);
        return new EntityFrameworkCoreDomainEventModuleRuntimeDescriptor(
            module,
            _domainEventPersistenceModules.Value.Contains(module.Name));
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
