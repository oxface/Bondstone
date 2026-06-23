using Bondstone.Diagnostics;
using Bondstone.Utility;

namespace Bondstone.Modules;

internal sealed class BondstoneModuleRegistry : IBondstoneModuleRegistry
{
    private readonly Dictionary<string, ModuleRegistrationState> _modules =
        new(StringComparer.Ordinal);

    public IReadOnlyCollection<BondstoneModuleRegistration> Modules
    {
        get
        {
            lock (_modules)
            {
                return _modules.Values
                    .Select(static state => state.ToRegistration())
                    .ToArray();
            }
        }
    }

    public BondstoneModuleRegistration GetModule(string moduleName)
    {
        if (TryGetModule(moduleName, out BondstoneModuleRegistration? registration))
        {
            return registration!;
        }

        throw new InvalidOperationException(
            $"Module '{moduleName}' is not registered.");
    }

    public bool TryGetModule(
        string moduleName,
        out BondstoneModuleRegistration? registration)
    {
        string normalizedModuleName = NormalizeModuleName(moduleName);

        lock (_modules)
        {
            if (_modules.TryGetValue(
                normalizedModuleName,
                out ModuleRegistrationState? state))
            {
                registration = state.ToRegistration();
                return true;
            }
        }

        registration = null;
        return false;
    }

    internal BondstoneModuleRegistration RegisterModule(string moduleName)
    {
        string normalizedModuleName = NormalizeModuleName(moduleName);

        lock (_modules)
        {
            if (!_modules.TryGetValue(
                normalizedModuleName,
                out ModuleRegistrationState? state))
            {
                state = new ModuleRegistrationState(normalizedModuleName);
                _modules.Add(normalizedModuleName, state);
            }

            return state.ToRegistration();
        }
    }

    internal BondstoneModuleRegistration EnableDurableMessaging(string moduleName)
    {
        string normalizedModuleName = NormalizeModuleName(moduleName);

        lock (_modules)
        {
            if (!_modules.TryGetValue(
                normalizedModuleName,
                out ModuleRegistrationState? state))
            {
                state = new ModuleRegistrationState(normalizedModuleName);
                _modules.Add(normalizedModuleName, state);
            }

            state.UsesDurableMessaging = true;
            return state.ToRegistration();
        }
    }

    internal BondstoneModuleRegistration EnablePersistence(
        string moduleName,
        string providerName,
        Type? contextType)
    {
        string normalizedModuleName = NormalizeModuleName(moduleName);
        string normalizedProviderName = providerName.NormalizeRequired(
            nameof(providerName),
            "Persistence provider name");

        lock (_modules)
        {
            if (!_modules.TryGetValue(
                normalizedModuleName,
                out ModuleRegistrationState? state))
            {
                state = new ModuleRegistrationState(normalizedModuleName);
                _modules.Add(normalizedModuleName, state);
            }

            state.EnablePersistence(normalizedProviderName, contextType);
            return state.ToRegistration();
        }
    }

    private static string NormalizeModuleName(string moduleName)
    {
        return moduleName.NormalizeRequired(nameof(moduleName), "Module name");
    }

    private sealed class ModuleRegistrationState(string name)
    {
        public string Name { get; } = name;

        public bool UsesDurableMessaging { get; set; }

        public string? PersistenceProviderName { get; private set; }

        public Type? PersistenceContextType { get; private set; }

        public void EnablePersistence(
            string providerName,
            Type? contextType)
        {
            if (PersistenceProviderName is not null
                && !StringComparer.Ordinal.Equals(PersistenceProviderName, providerName))
            {
                throw new BondstoneSetupException(
                    BondstoneSetupCodes.DuplicateDurableRegistration,
                    $"Module '{Name}' already uses persistence provider '{PersistenceProviderName}'.");
            }

            if (PersistenceContextType is not null
                && contextType is not null
                && PersistenceContextType != contextType)
            {
                throw new BondstoneSetupException(
                    BondstoneSetupCodes.DuplicateDurableRegistration,
                    $"Module '{Name}' already uses persistence context '{PersistenceContextType.FullName}'.");
            }

            PersistenceProviderName ??= providerName;
            PersistenceContextType ??= contextType;
        }

        public BondstoneModuleRegistration ToRegistration()
        {
            return new BondstoneModuleRegistration(
                Name,
                UsesDurableMessaging,
                PersistenceProviderName,
                PersistenceContextType);
        }
    }
}
