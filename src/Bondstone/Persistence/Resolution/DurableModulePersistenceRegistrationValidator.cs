using Bondstone.Utility;

namespace Bondstone.Persistence;

internal static class DurableModulePersistenceRegistrationValidator
{
    public static TRegistration[] ToValidatedArray<TRegistration>(
        IEnumerable<TRegistration> registrations,
        Func<TRegistration, string> moduleNameSelector,
        string registrationDescription)
    {
        ArgumentNullException.ThrowIfNull(registrations);
        ArgumentNullException.ThrowIfNull(moduleNameSelector);

        TRegistration[] array = registrations.ToArray();
        string[] duplicateModuleNames = array
            .Select(registration => moduleNameSelector(registration).NormalizeRequired(
                "moduleName",
                "Module name"))
            .GroupBy(static moduleName => moduleName, StringComparer.Ordinal)
            .Where(static group => group.Count() > 1)
            .Select(static group => group.Key)
            .OrderBy(static moduleName => moduleName, StringComparer.Ordinal)
            .ToArray();

        if (duplicateModuleNames.Length == 0)
        {
            return array;
        }

        throw new InvalidOperationException(
            $"Multiple {registrationDescription} registrations exist for module(s): '{string.Join("', '", duplicateModuleNames)}'. Configure exactly one {registrationDescription} per module.");
    }
}
