using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableInboxInspector : IDurableInboxInspector
{
    private readonly ModuleRuntimeRegistry _moduleRuntimeRegistry;

    public DurableInboxInspector(ModuleRuntimeRegistry moduleRuntimeRegistry)
    {
        _moduleRuntimeRegistry =
            moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));
        _moduleRuntimeRegistry.ValidateDurableInboxInspectionStores();
    }

    public async ValueTask<IReadOnlyList<DurableInboxRecord>> FindUnprocessedAsync(
        string moduleName,
        int maxCount = 100,
        DateTimeOffset? receivedAtOrBeforeUtc = null,
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
                    "durable module inbox inspection store"));
        }

        if (runtime.TryGetDurableInboxInspectionStore(
                out IDurableInboxInspectionStore? store)
            && store is not null)
        {
            return await store.FindUnprocessedAsync(
                maxCount,
                receivedAtOrBeforeUtc,
                normalizedModuleName,
                ct);
        }

        throw new InvalidOperationException(
            DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                _moduleRuntimeRegistry,
                runtime.ModuleName,
                "durable module inbox inspection store"));
    }
}
