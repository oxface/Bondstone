using System.Diagnostics;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Messaging;

internal sealed class DurableOperationFinalizer(
    ModuleRuntimeRegistry moduleRuntimeRegistry,
    TimeProvider? timeProvider = null)
    : IDurableOperationFinalizer
{
    private readonly ModuleRuntimeRegistry _moduleRuntimeRegistry =
        moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async ValueTask<DurableOperationFinalizationResult> MarkFailedAsync(
        string moduleName,
        Guid durableOperationId,
        string failureReason,
        DurableOperationDiagnosticContext? diagnosticContext = null,
        CancellationToken ct = default)
    {
        return await MarkTerminalAsync(
            moduleName,
            durableOperationId,
            DurableOperationStatus.Failed,
            failureReason,
            diagnosticContext,
            ct);
    }

    public async ValueTask<DurableOperationFinalizationResult> MarkCancelledAsync(
        string moduleName,
        Guid durableOperationId,
        string cancellationReason,
        DurableOperationDiagnosticContext? diagnosticContext = null,
        CancellationToken ct = default)
    {
        return await MarkTerminalAsync(
            moduleName,
            durableOperationId,
            DurableOperationStatus.Cancelled,
            cancellationReason,
            diagnosticContext,
            ct);
    }

    private async ValueTask<DurableOperationFinalizationResult> MarkTerminalAsync(
        string moduleName,
        Guid durableOperationId,
        DurableOperationStatus terminalStatus,
        string reason,
        DurableOperationDiagnosticContext? diagnosticContext,
        CancellationToken ct)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");
        ValidateOperationId(durableOperationId);
        string normalizedReason = reason.NormalizeRequired(
            nameof(reason),
            "Operation finalization reason");

        using Activity? activity = BondstoneMessagingDiagnostics.ActivitySource.StartActivity(
            BondstoneMessagingDiagnostics.OperationFinalizeActivityName,
            ActivityKind.Internal);
        activity?.SetTag(
            BondstoneMessagingDiagnostics.Tags.Module,
            normalizedModuleName);
        try
        {
            IDurableOperationStateStore store = ResolveStore(
                normalizedModuleName,
                durableOperationId);
            DurableOperationState? currentState = await store.GetStateAsync(
                durableOperationId,
                ct);

            if (currentState is not null && IsTerminal(currentState.Status))
            {
                activity?.SetTag(
                    BondstoneMessagingDiagnostics.Tags.OperationStatus,
                    currentState.Status.ToString());
                activity?.SetTag(
                    BondstoneMessagingDiagnostics.Tags.OperationFinalized,
                    false);

                return new DurableOperationFinalizationResult(
                    currentState,
                    wasFinalized: false);
            }

            var terminalState = new DurableOperationState(
                durableOperationId,
                terminalStatus,
                _timeProvider.GetUtcNow(),
                failureReason: normalizedReason,
                diagnosticContext: diagnosticContext ?? currentState?.DiagnosticContext);

            await store.SaveAsync(
                terminalState,
                ct);

            activity?.SetTag(
                BondstoneMessagingDiagnostics.Tags.OperationStatus,
                terminalState.Status.ToString());
            activity?.SetTag(
                BondstoneMessagingDiagnostics.Tags.OperationFinalized,
                true);
            BondstoneMessagingDiagnostics.RecordOperationFinalized(
                normalizedModuleName,
                terminalState.Status);

            return new DurableOperationFinalizationResult(
                terminalState,
                wasFinalized: true);
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
    }

    private IDurableOperationStateStore ResolveStore(
        string moduleName,
        Guid durableOperationId)
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
                $"Durable operation id '{durableOperationId}' requires {nameof(IDurableOperationStateStore)} for explicit operation finalization. {missingModuleMessage}");
        }

        return store;
    }

    private static void ValidateOperationId(Guid durableOperationId)
    {
        if (durableOperationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Durable operation id must not be empty.",
                nameof(durableOperationId));
        }
    }

    private static bool IsTerminal(DurableOperationStatus status) =>
        status is DurableOperationStatus.Completed
            or DurableOperationStatus.Failed
            or DurableOperationStatus.Cancelled;
}
