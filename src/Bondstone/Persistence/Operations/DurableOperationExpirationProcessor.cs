using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Messaging;

internal sealed class DurableOperationExpirationProcessor(
    ModuleRuntimeRegistry moduleRuntimeRegistry,
    IDurableOperationFinalizer operationFinalizer)
    : IDurableOperationExpirationProcessor
{
    private readonly ModuleRuntimeRegistry _moduleRuntimeRegistry =
        moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));
    private readonly IDurableOperationFinalizer _operationFinalizer =
        operationFinalizer ?? throw new ArgumentNullException(nameof(operationFinalizer));

    public async ValueTask<DurableOperationExpirationResult> MarkExpiredAsync(
        string moduleName,
        DateTimeOffset expiresBeforeUtc,
        DurableOperationStatus terminalStatus,
        string reason,
        int maxCount = 100,
        DurableOperationDiagnosticContext? diagnosticContext = null,
        CancellationToken ct = default)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");
        ValidateCutoff(expiresBeforeUtc);
        ValidateTerminalStatus(terminalStatus);
        string normalizedReason = reason.NormalizeRequired(
            nameof(reason),
            "Operation expiry reason");
        ValidateMaxCount(maxCount);

        IDurableOperationExpirationStore expirationStore = ResolveExpirationStore(
            normalizedModuleName);
        IReadOnlyList<DurableOperationState> candidates =
            await expirationStore.FindExpirationCandidatesAsync(
                expiresBeforeUtc,
                maxCount,
                ct);

        var finalizations = new List<DurableOperationFinalizationResult>(candidates.Count);
        foreach (DurableOperationState candidate in candidates)
        {
            DurableOperationFinalizationResult finalization = terminalStatus switch
            {
                DurableOperationStatus.Failed => await _operationFinalizer.MarkFailedAsync(
                    normalizedModuleName,
                    candidate.DurableOperationId,
                    normalizedReason,
                    diagnosticContext,
                    ct),
                DurableOperationStatus.Cancelled => await _operationFinalizer.MarkCancelledAsync(
                    normalizedModuleName,
                    candidate.DurableOperationId,
                    normalizedReason,
                    diagnosticContext,
                    ct),
                _ => throw new InvalidOperationException("Unsupported expiry terminal status."),
            };
            finalizations.Add(finalization);
        }

        return new DurableOperationExpirationResult(
            normalizedModuleName,
            expiresBeforeUtc,
            terminalStatus,
            finalizations);
    }

    private IDurableOperationExpirationStore ResolveExpirationStore(
        string moduleName)
    {
        _moduleRuntimeRegistry.ValidateDurableOperationStateStores();

        if (!_moduleRuntimeRegistry.HasDurableOperationStateStores
            || !_moduleRuntimeRegistry.TryGetRuntime(
                moduleName,
                out ModuleRuntimeDescriptor? runtime)
            || runtime is null
            || !runtime.TryGetDurableOperationStateStore(
                out IDurableOperationStateStore? store)
            || store is null)
        {
            string missingModuleMessage =
                DurableModulePersistenceDiagnosticFormatter.MissingModuleRegistration(
                    _moduleRuntimeRegistry,
                    moduleName,
                    "durable module operation-state store");
            throw new InvalidOperationException(
                $"Operation expiry requires {nameof(IDurableOperationStateStore)}. {missingModuleMessage}");
        }

        if (store is IDurableOperationExpirationStore expirationStore)
        {
            return expirationStore;
        }

        throw new InvalidOperationException(
            $"Operation expiry for module '{moduleName}' requires its durable operation-state store to implement {nameof(IDurableOperationExpirationStore)}.");
    }

    private static void ValidateCutoff(DateTimeOffset expiresBeforeUtc)
    {
        if (expiresBeforeUtc == default)
        {
            throw new ArgumentException("Expiry cutoff must not be the default value.", nameof(expiresBeforeUtc));
        }

        if (expiresBeforeUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Expiry cutoff must use UTC offset.", nameof(expiresBeforeUtc));
        }
    }

    private static void ValidateTerminalStatus(DurableOperationStatus terminalStatus)
    {
        if (terminalStatus is DurableOperationStatus.Failed or DurableOperationStatus.Cancelled)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(
            nameof(terminalStatus),
            terminalStatus,
            "Expiry terminal status must be Failed or Cancelled.");
    }

    private static void ValidateMaxCount(int maxCount)
    {
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCount),
                maxCount,
                "Maximum expiry count must be greater than zero.");
        }
    }
}
