using Bondstone.Utility;

namespace Bondstone.Modules;

internal sealed class ModuleCommandValidatorRegistry
{
    private readonly object _syncRoot = new();
    private readonly List<ModuleCommandValidatorRegistration> _registrations = [];

    public void Add(ModuleCommandValidatorRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_syncRoot)
        {
            if (_registrations.Any(existing =>
                StringComparer.Ordinal.Equals(existing.ModuleName, registration.ModuleName)
                && existing.CommandType == registration.CommandType
                && existing.ValidatorType == registration.ValidatorType))
            {
                return;
            }

            _registrations.Add(registration);
        }
    }

    public IReadOnlyList<ModuleCommandValidatorRegistration> GetValidators(
        string moduleName,
        Type commandType)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        lock (_syncRoot)
        {
            return _registrations
                .Where(registration => StringComparer.Ordinal.Equals(
                        registration.ModuleName,
                        normalizedModuleName)
                    && registration.CommandType == commandType)
                .ToArray();
        }
    }
}
