using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableOutboxInspector : IDurableOutboxInspector
{
    private readonly ModuleRuntimeRegistry _moduleRuntimeRegistry;

    public DurableOutboxInspector(ModuleRuntimeRegistry moduleRuntimeRegistry)
    {
        _moduleRuntimeRegistry =
            moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));
        _moduleRuntimeRegistry.ValidateDurableOutboxInspectionStores();
    }

    public async ValueTask<IReadOnlyList<DurableOutboxRecord>> FindTerminalFailedAsync(
        string moduleName,
        int maxCount = 100,
        DateTimeOffset? failedAtOrBeforeUtc = null,
        CancellationToken ct = default)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCount),
                maxCount,
                "Maximum inspection count must be positive.");
        }

        if (!_moduleRuntimeRegistry.TryGetRuntime(
                normalizedModuleName,
                out ModuleRuntimeDescriptor? runtime)
            || runtime is null)
        {
            throw new InvalidOperationException(
                DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                    _moduleRuntimeRegistry,
                    normalizedModuleName,
                    "durable module outbox inspection store"));
        }

        if (runtime.TryGetDurableOutboxInspectionStore(
                out IDurableOutboxInspectionStore? store)
            && store is not null)
        {
            return await store.FindTerminalFailedAsync(
                maxCount,
                failedAtOrBeforeUtc,
                normalizedModuleName,
                ct);
        }

        throw new InvalidOperationException(
            DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                _moduleRuntimeRegistry,
                runtime.ModuleName,
                "durable module outbox inspection store"));
    }
}
