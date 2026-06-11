using Bondstone.EntityFrameworkCore.DomainEvents;
using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.EntityFrameworkCore.Persistence;

internal sealed class EntityFrameworkCoreModuleRuntimeRegistry(
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

    public EntityFrameworkCoreModuleRuntimeDescriptor GetRuntime(string moduleName)
    {
        BondstoneModuleRegistration module = _moduleRegistry.GetModule(moduleName);
        return new EntityFrameworkCoreModuleRuntimeDescriptor(
            module,
            _domainEventPersistenceModules.Value.Contains(module.Name));
    }
}
