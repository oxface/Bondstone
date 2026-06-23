namespace Bondstone.Persistence;

/// <summary>
/// Provides provider-side read access to durable incoming inbox rows.
/// </summary>
/// <remarks>
/// This is a provider/runtime contract for inspection. It must not mutate
/// durable incoming inbox records, inbox rows, broker messages, or operation state.
/// </remarks>
public interface IDurableIncomingInboxInspectionStore
{
    /// <summary>
    /// Finds durable incoming inbox records from the underlying store.
    /// </summary>
    /// <param name="status">An optional durable incoming inbox status filter.</param>
    /// <param name="maxCount">The maximum number of records to return.</param>
    /// <param name="ingestedAtOrBeforeUtc">An optional UTC ingested-at cutoff.</param>
    /// <param name="receiverModule">An optional receiver-module filter applied by the provider store.</param>
    /// <param name="sourceTransportName">An optional source transport diagnostic-name filter.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The matching durable incoming inbox records.</returns>
    ValueTask<IReadOnlyList<DurableIncomingInboxRecord>> FindAsync(
        DurableIncomingInboxStatus? status = null,
        int maxCount = 100,
        DateTimeOffset? ingestedAtOrBeforeUtc = null,
        string? receiverModule = null,
        string? sourceTransportName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Finds processing records whose claim lease has expired at or before the supplied UTC cutoff.
    /// </summary>
    /// <param name="claimedUntilAtOrBeforeUtc">The UTC claim lease expiration cutoff.</param>
    /// <param name="maxCount">The maximum number of records to return.</param>
    /// <param name="receiverModule">An optional receiver-module filter applied by the provider store.</param>
    /// <param name="sourceTransportName">An optional source transport diagnostic-name filter.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The matching stale processing records.</returns>
    ValueTask<IReadOnlyList<DurableIncomingInboxRecord>> FindStaleProcessingAsync(
        DateTimeOffset claimedUntilAtOrBeforeUtc,
        int maxCount = 100,
        string? receiverModule = null,
        string? sourceTransportName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Finds terminal receive-failure records from the underlying store.
    /// </summary>
    /// <param name="maxCount">The maximum number of records to return.</param>
    /// <param name="failedAtOrBeforeUtc">An optional UTC failed-at cutoff.</param>
    /// <param name="receiverModule">An optional receiver-module filter applied by the provider store.</param>
    /// <param name="sourceTransportName">An optional source transport diagnostic-name filter.</param>
    /// <param name="ct">A cancellation token for the read operation.</param>
    /// <returns>The matching terminal receive-failure records.</returns>
    ValueTask<IReadOnlyList<DurableIncomingInboxRecord>> FindTerminalFailedAsync(
        int maxCount = 100,
        DateTimeOffset? failedAtOrBeforeUtc = null,
        string? receiverModule = null,
        string? sourceTransportName = null,
        CancellationToken ct = default);
}
