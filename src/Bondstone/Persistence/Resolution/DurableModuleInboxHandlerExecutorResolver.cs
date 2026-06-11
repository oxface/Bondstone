using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableModuleInboxHandlerExecutorResolver
{
    private readonly IDurableInboxHandlerExecutor? _fallbackExecutor;
    private readonly ModuleRuntimeRegistry _moduleRuntimeRegistry;

    public DurableModuleInboxHandlerExecutorResolver(
        IDurableInboxHandlerExecutor? fallbackExecutor,
        ModuleRuntimeRegistry moduleRuntimeRegistry)
    {
        _fallbackExecutor = fallbackExecutor;
        _moduleRuntimeRegistry =
            moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));
        _moduleRuntimeRegistry.ValidateDurableInboxHandlerExecutors();
    }

    public IDurableInboxHandlerExecutor Resolve(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (!_moduleRuntimeRegistry.HasDurableInboxHandlerExecutors
            && _fallbackExecutor is not null)
        {
            return _fallbackExecutor;
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
                    "durable module inbox handler executor"));
        }

        if (runtime.TryGetDurableInboxHandlerExecutor(
                out IDurableInboxHandlerExecutor? executor)
            && executor is not null)
        {
            return executor;
        }

        throw new InvalidOperationException(
            DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                _moduleRuntimeRegistry,
                runtime.ModuleName,
                "durable module inbox handler executor"));
    }
}
