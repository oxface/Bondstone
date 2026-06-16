namespace Bondstone.Persistence;

/// <summary>
/// Provides provider-side read access to terminal outbox rows.
/// </summary>
/// <remarks>
/// This is a provider/runtime contract used by module persistence packages.
/// Application code should normally use <see cref="IDurableOutboxInspector"/>.
/// </remarks>
public interface IDurableOutboxInspectionStore
{
    /// <summary>
    /// Finds terminal failed outbox records from the underlying store.
    /// </summary>
    /// <param name="maxCount">The maximum number of terminal failed rows to return.</param>
    /// <param name="failedAtOrBeforeUtc">An optional UTC failed-at cutoff.</param>
    /// <param name="sourceModuleName">An optional source-module filter applied by the provider store.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The matching terminal failed outbox records.</returns>
    ValueTask<IReadOnlyList<DurableOutboxRecord>> FindTerminalFailedAsync(
        int maxCount = 100,
        DateTimeOffset? failedAtOrBeforeUtc = null,
        string? sourceModuleName = null,
        CancellationToken ct = default);
}
