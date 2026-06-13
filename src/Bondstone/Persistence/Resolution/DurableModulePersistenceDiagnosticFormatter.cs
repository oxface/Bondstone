using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Persistence;

internal static class DurableModulePersistenceDiagnosticFormatter
{
    public static string MissingModuleRegistration(
        ModuleRuntimeRegistry moduleRuntimeRegistry,
        string moduleName,
        string registrationDescription)
    {
        ArgumentNullException.ThrowIfNull(moduleRuntimeRegistry);

        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (!moduleRuntimeRegistry.TryGetRuntime(
                normalizedModuleName,
                out ModuleRuntimeDescriptor? runtime)
            || runtime is null)
        {
            return $"No {registrationDescription} is registered for module '{normalizedModuleName}', and the module is not registered in Bondstone.";
        }

        BondstoneModuleRegistration module = runtime.Module;
        if (!module.UsesPersistence)
        {
            return $"No {registrationDescription} is registered for module '{normalizedModuleName}', and the module does not declare persistence.";
        }

        string contextDescription = module.PersistenceContextType is null
            ? string.Empty
            : $" with context '{module.PersistenceContextType.FullName}'";

        return $"No {registrationDescription} is registered for module '{normalizedModuleName}'. Module '{normalizedModuleName}' declares persistence provider '{module.PersistenceProviderName}'{contextDescription}; configure that provider's module persistence services for this module.";
    }
}
