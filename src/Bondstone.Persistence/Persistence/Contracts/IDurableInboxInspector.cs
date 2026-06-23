namespace Bondstone.Persistence;

/// <summary>
/// Reads operator-facing inbox inspection data from a named module boundary.
/// </summary>
public interface IDurableInboxInspector
{
    /// <summary>
    /// Finds inbox records that have been received but not yet marked processed for one module.
    /// </summary>
    /// <param name="moduleName">The module whose inbox persistence boundary should be inspected.</param>
    /// <param name="maxCount">The maximum number of unprocessed rows to return.</param>
    /// <param name="receivedAtOrBeforeUtc">An optional UTC received-at cutoff. When omitted, rows are not time-filtered.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The matching unprocessed inbox records ordered for operational inspection.</returns>
    ValueTask<IReadOnlyList<DurableInboxRecord>> FindUnprocessedAsync(
        string moduleName,
        int maxCount = 100,
        DateTimeOffset? receivedAtOrBeforeUtc = null,
        CancellationToken ct = default);
}
