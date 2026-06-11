using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableModuleOutboxWriterResolver
{
    private readonly IDurableOutboxWriter? _fallbackWriter;
    private readonly ModuleRuntimeRegistry _moduleRuntimeRegistry;

    public DurableModuleOutboxWriterResolver(
        IDurableOutboxWriter? fallbackWriter,
        ModuleRuntimeRegistry moduleRuntimeRegistry)
    {
        _fallbackWriter = fallbackWriter;
        _moduleRuntimeRegistry =
            moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));
        _moduleRuntimeRegistry.ValidateDurableOutboxWriters();
    }

    public IDurableOutboxWriter Resolve(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        if (!_moduleRuntimeRegistry.HasDurableOutboxWriters && _fallbackWriter is not null)
        {
            return _fallbackWriter;
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
                    "durable module outbox writer"));
        }

        if (runtime.TryGetDurableOutboxWriter(out IDurableOutboxWriter? writer)
            && writer is not null)
        {
            return writer;
        }

        throw new InvalidOperationException(
            DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                _moduleRuntimeRegistry,
                runtime.ModuleName,
                "durable module outbox writer"));
    }
}
