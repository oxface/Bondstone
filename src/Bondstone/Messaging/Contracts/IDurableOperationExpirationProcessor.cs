namespace Bondstone.Messaging;

/// <summary>
/// Applies application-owned durable operation expiry policy to module-owned operation state.
/// </summary>
public interface IDurableOperationExpirationProcessor
{
    /// <summary>
    /// Marks stale non-terminal operations in one module as failed or cancelled.
    /// </summary>
    /// <param name="moduleName">The module that owns the operation-state store to scan and update.</param>
    /// <param name="expiresBeforeUtc">The UTC cutoff for candidate operation-state update timestamps.</param>
    /// <param name="terminalStatus">The terminal status to write. Must be <see cref="DurableOperationStatus.Failed"/> or <see cref="DurableOperationStatus.Cancelled"/>.</param>
    /// <param name="reason">The application-owned terminal outcome reason.</param>
    /// <param name="maxCount">The maximum number of candidates to process.</param>
    /// <param name="diagnosticContext">Optional diagnostic context to write with the terminal state.</param>
    /// <param name="ct">A cancellation token for the expiry operation.</param>
    /// <returns>The expiry processing result.</returns>
    ValueTask<DurableOperationExpirationResult> MarkExpiredAsync(
        string moduleName,
        DateTimeOffset expiresBeforeUtc,
        DurableOperationStatus terminalStatus,
        string reason,
        int maxCount = 100,
        DurableOperationDiagnosticContext? diagnosticContext = null,
        CancellationToken ct = default);
}
