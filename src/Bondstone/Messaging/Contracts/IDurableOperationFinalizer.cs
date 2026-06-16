namespace Bondstone.Messaging;

/// <summary>
/// Marks durable operations with explicit application-owned terminal outcomes.
/// </summary>
public interface IDurableOperationFinalizer
{
    /// <summary>
    /// Marks an operation as failed in the owning module's operation-state store.
    /// </summary>
    /// <param name="moduleName">The module that owns the operation-state store to update.</param>
    /// <param name="durableOperationId">The durable operation identifier to finalize.</param>
    /// <param name="failureReason">The application-owned reason for the failed outcome.</param>
    /// <param name="diagnosticContext">Optional diagnostic context to store with the terminal state.</param>
    /// <param name="ct">A cancellation token for the persistence operation.</param>
    /// <returns>The finalization outcome and resulting operation state.</returns>
    ValueTask<DurableOperationFinalizationResult> MarkFailedAsync(
        string moduleName,
        Guid durableOperationId,
        string failureReason,
        DurableOperationDiagnosticContext? diagnosticContext = null,
        CancellationToken ct = default);

    /// <summary>
    /// Marks an operation as cancelled in the owning module's operation-state store.
    /// </summary>
    /// <param name="moduleName">The module that owns the operation-state store to update.</param>
    /// <param name="durableOperationId">The durable operation identifier to finalize.</param>
    /// <param name="cancellationReason">The application-owned reason for the cancelled outcome.</param>
    /// <param name="diagnosticContext">Optional diagnostic context to store with the terminal state.</param>
    /// <param name="ct">A cancellation token for the persistence operation.</param>
    /// <returns>The finalization outcome and resulting operation state.</returns>
    ValueTask<DurableOperationFinalizationResult> MarkCancelledAsync(
        string moduleName,
        Guid durableOperationId,
        string cancellationReason,
        DurableOperationDiagnosticContext? diagnosticContext = null,
        CancellationToken ct = default);
}
