using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Persistence;

internal sealed class DurableIncomingInboxIngestionBoundaryResolver
    : IDurableIncomingInboxIngestionBoundaryResolver
{
    private readonly Func<DurableIncomingInboxIngestionBoundary?> _fallbackBoundaryFactory;
    private readonly ModuleRuntimeRegistry _moduleRuntimeRegistry;

    public DurableIncomingInboxIngestionBoundaryResolver(
        Func<DurableIncomingInboxIngestionBoundary?> fallbackBoundaryFactory,
        ModuleRuntimeRegistry moduleRuntimeRegistry)
    {
        _fallbackBoundaryFactory = fallbackBoundaryFactory
            ?? throw new ArgumentNullException(nameof(fallbackBoundaryFactory));
        _moduleRuntimeRegistry =
            moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));
        _moduleRuntimeRegistry.ValidateDurableIncomingInboxIngestionBoundaries();
    }

    public DurableIncomingInboxIngestionBoundary Resolve(string receiverModule)
    {
        string normalizedReceiverModule = receiverModule.NormalizeRequired(
            nameof(receiverModule),
            "Receiver module");

        if (!_moduleRuntimeRegistry.HasDurableIncomingInboxIngestionBoundaries
            && !_moduleRuntimeRegistry.HasDurableModulePersistenceRegistrations
            && _fallbackBoundaryFactory() is DurableIncomingInboxIngestionBoundary fallbackBoundary)
        {
            return fallbackBoundary;
        }

        if (!_moduleRuntimeRegistry.TryGetRuntime(
                normalizedReceiverModule,
                out ModuleRuntimeDescriptor? runtime)
            || runtime is null)
        {
            throw new InvalidOperationException(
                DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                    _moduleRuntimeRegistry,
                    normalizedReceiverModule,
                    "durable module incoming inbox ingestion boundary"));
        }

        if (runtime.TryGetDurableIncomingInboxIngestionBoundary(
                out DurableIncomingInboxIngestionBoundary? boundary)
            && boundary is not null)
        {
            return boundary;
        }

        throw new InvalidOperationException(
            DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                _moduleRuntimeRegistry,
                runtime.ModuleName,
                "durable module incoming inbox ingestion boundary"));
    }
}
