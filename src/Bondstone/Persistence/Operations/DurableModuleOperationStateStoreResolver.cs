using Bondstone.Utility;
using Bondstone.Modules;

namespace Bondstone.Persistence;

internal sealed class DurableModuleOperationStateStoreResolver(
    IEnumerable<IDurableModuleOperationStateStore> moduleStores,
    IDurableOperationStateStore? fallbackStore,
    IBondstoneModuleRegistry moduleRegistry)
{
    private readonly IDurableModuleOperationStateStore[] _moduleStores =
        DurableModulePersistenceRegistrationValidator.ToValidatedArray(
            moduleStores,
            static store => store.ModuleName,
            "durable module operation-state store");
    private readonly IBondstoneModuleRegistry _moduleRegistry =
        moduleRegistry ?? throw new ArgumentNullException(nameof(moduleRegistry));

    public IDurableOperationStateStore Resolve(
        string moduleName,
        Guid durableOperationId)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        IDurableModuleOperationStateStore? moduleStore = _moduleStores
            .SingleOrDefault(store => StringComparer.Ordinal.Equals(
                store.ModuleName.NormalizeRequired(
                    nameof(IDurableModuleOperationStateStore.ModuleName),
                    "Module name"),
                normalizedModuleName));

        if (moduleStore is not null)
        {
            return moduleStore;
        }

        if (_moduleStores.Length == 0 && fallbackStore is not null)
        {
            return fallbackStore;
        }

        string message = DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
            _moduleRegistry,
            normalizedModuleName,
            "durable module operation-state store");
        throw new InvalidOperationException(
            $"Durable operation id '{durableOperationId}' requires {nameof(IDurableOperationStateStore)}. {message}");
    }
}
