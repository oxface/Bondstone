namespace Bondstone.Persistence;

/// <summary>
/// Provides provider-side read access to received-but-unprocessed inbox rows.
/// </summary>
/// <remarks>
/// This is a provider/runtime contract used by module persistence packages.
/// Application code should normally use <see cref="IDurableInboxInspector"/>.
/// </remarks>
public interface IDurableInboxInspectionStore
{
    /// <summary>
    /// Finds received-but-unprocessed inbox records from the underlying store.
    /// </summary>
    /// <param name="maxCount">The maximum number of unprocessed rows to return.</param>
    /// <param name="receivedAtOrBeforeUtc">An optional UTC received-at cutoff.</param>
    /// <param name="moduleName">An optional module filter applied by the provider store.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The matching unprocessed inbox records.</returns>
    ValueTask<IReadOnlyList<DurableInboxRecord>> FindUnprocessedAsync(
        int maxCount = 100,
        DateTimeOffset? receivedAtOrBeforeUtc = null,
        string? moduleName = null,
        CancellationToken ct = default);
}
