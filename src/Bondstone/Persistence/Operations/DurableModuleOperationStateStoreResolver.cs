using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableModuleOperationStateStoreResolver
{
    private readonly IDurableOperationStateStore? _fallbackStore;
    private readonly ModuleRuntimeRegistry _moduleRuntimeRegistry;

    public DurableModuleOperationStateStoreResolver(
        IDurableOperationStateStore? fallbackStore,
        ModuleRuntimeRegistry moduleRuntimeRegistry)
    {
        _fallbackStore = fallbackStore;
        _moduleRuntimeRegistry =
            moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));
        _moduleRuntimeRegistry.ValidateDurableOperationStateStores();
    }

    public IDurableOperationStateStore Resolve(
        string moduleName,
        Guid durableOperationId)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (!_moduleRuntimeRegistry.HasDurableOperationStateStores && _fallbackStore is not null)
        {
            return _fallbackStore;
        }

        if (!_moduleRuntimeRegistry.TryGetRuntime(
                normalizedModuleName,
                out ModuleRuntimeDescriptor? runtime)
            || runtime is null)
        {
            string missingModuleMessage =
                DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                    _moduleRuntimeRegistry,
                    normalizedModuleName,
                    "durable module operation-state store");
            throw new InvalidOperationException(
                $"Durable operation id '{durableOperationId}' requires {nameof(IDurableOperationStateStore)}. {missingModuleMessage}");
        }

        if (runtime.TryGetDurableOperationStateStore(
                out IDurableOperationStateStore? store)
            && store is not null)
        {
            return store;
        }

        string message = DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
            _moduleRuntimeRegistry,
            runtime.ModuleName,
            "durable module operation-state store");
        throw new InvalidOperationException(
            $"Durable operation id '{durableOperationId}' requires {nameof(IDurableOperationStateStore)}. {message}");
    }
}
