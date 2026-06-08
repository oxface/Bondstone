using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableModuleOperationStateStoreResolver(
    IEnumerable<IDurableModuleOperationStateStore> moduleStores,
    IDurableOperationStateStore? fallbackStore)
{
    private readonly IDurableModuleOperationStateStore[] _moduleStores =
        moduleStores?.ToArray() ?? throw new ArgumentNullException(nameof(moduleStores));

    public IDurableOperationStateStore Resolve(
        string moduleName,
        Guid durableOperationId)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        IDurableModuleOperationStateStore? moduleStore = _moduleStores
            .SingleOrDefault(store => StringComparer.Ordinal.Equals(
                store.ModuleName,
                normalizedModuleName));

        if (moduleStore is not null)
        {
            return moduleStore;
        }

        if (_moduleStores.Length == 0 && fallbackStore is not null)
        {
            return fallbackStore;
        }

        throw new InvalidOperationException(
            $"Durable operation id '{durableOperationId}' requires {nameof(IDurableOperationStateStore)} "
            + $"for module '{normalizedModuleName}'.");
    }
}
