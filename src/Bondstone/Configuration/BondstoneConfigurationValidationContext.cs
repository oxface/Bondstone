using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Configuration;

public sealed class BondstoneConfigurationValidationContext
{
    internal BondstoneConfigurationValidationContext(
        IReadOnlyCollection<BondstoneModuleRegistration> modules,
        IReadOnlyCollection<ModuleCommandRoute> commandRoutes)
    {
        Modules = modules;
        CommandRoutes = commandRoutes;
        ModulesByName = modules.ToDictionary(
            static module => module.Name,
            StringComparer.Ordinal);
        DurableCommandRoutes = commandRoutes
            .Where(static route => route.IsDurable)
            .ToArray();
    }

    public IReadOnlyCollection<BondstoneModuleRegistration> Modules { get; }

    public IReadOnlyDictionary<string, BondstoneModuleRegistration> ModulesByName { get; }

    public IReadOnlyCollection<ModuleCommandRoute> CommandRoutes { get; }

    public IReadOnlyCollection<ModuleCommandRoute> DurableCommandRoutes { get; }

    public bool ModuleHasDurableCommandHandlers(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        return DurableCommandRoutes.Any(route =>
            route.ModuleName == normalizedModuleName);
    }
}
