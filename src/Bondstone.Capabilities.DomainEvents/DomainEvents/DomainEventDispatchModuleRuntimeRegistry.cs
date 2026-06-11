using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Capabilities.DomainEvents;

internal sealed class DomainEventDispatchModuleRuntimeRegistry(
    IBondstoneModuleRegistry moduleRegistry,
    IEnumerable<DomainEventDispatchModule> domainEventDispatchModules)
{
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));
    private readonly Lazy<IReadOnlySet<string>> _domainEventDispatchModules =
        new(() => domainEventDispatchModules
            .Select(static module => module.ModuleName.NormalizeRequired(
                nameof(DomainEventDispatchModule.ModuleName),
                "Module name"))
            .ToHashSet(StringComparer.Ordinal));

    public DomainEventDispatchModuleRuntimeDescriptor GetRuntime(string moduleName)
    {
        BondstoneModuleRegistration module = _moduleRegistry.GetModule(moduleName);
        return new DomainEventDispatchModuleRuntimeDescriptor(
            module,
            _domainEventDispatchModules.Value.Contains(module.Name));
    }
}

internal sealed class DomainEventDispatchModuleRuntimeDescriptor(
    BondstoneModuleRegistration module,
    bool usesDomainEventDispatch)
{
    public BondstoneModuleRegistration Module { get; } =
        module ?? throw new ArgumentNullException(nameof(module));

    public bool UsesDomainEventDispatch { get; } = usesDomainEventDispatch;
}
