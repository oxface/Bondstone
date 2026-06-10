using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Persistence;

internal static class DurableModulePersistenceDiagnosticFormatter
{
    public static string MissingModuleRegistration(
        IBondstoneModuleRegistry moduleRegistry,
        string moduleName,
        string registrationDescription)
    {
        ArgumentNullException.ThrowIfNull(moduleRegistry);

        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (!moduleRegistry.TryGetModule(
                normalizedModuleName,
                out BondstoneModuleRegistration? module)
            || module is null)
        {
            return $"No {registrationDescription} is registered for module '{normalizedModuleName}', and the module is not registered in Bondstone.";
        }

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
