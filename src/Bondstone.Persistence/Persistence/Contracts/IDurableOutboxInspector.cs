namespace Bondstone.Persistence;

/// <summary>
/// Reads operator-facing outbox inspection data from a named module boundary.
/// </summary>
public interface IDurableOutboxInspector
{
    /// <summary>
    /// Finds outbox records that reached the terminal failed state for one module.
    /// </summary>
    /// <param name="moduleName">The module whose outbox persistence boundary should be inspected.</param>
    /// <param name="maxCount">The maximum number of terminal failed rows to return.</param>
    /// <param name="failedAtOrBeforeUtc">An optional UTC failed-at cutoff. When omitted, the newest eligible rows are not time-filtered.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The matching terminal failed outbox records ordered for operational inspection.</returns>
    ValueTask<IReadOnlyList<DurableOutboxRecord>> FindTerminalFailedAsync(
        string moduleName,
        int maxCount = 100,
        DateTimeOffset? failedAtOrBeforeUtc = null,
        CancellationToken ct = default);
}
