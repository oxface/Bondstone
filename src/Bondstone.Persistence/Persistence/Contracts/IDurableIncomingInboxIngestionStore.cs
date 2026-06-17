namespace Bondstone.Persistence;

/// <summary>
/// Persists durable incoming inbox records idempotently.
/// </summary>
/// <remarks>
/// This is a provider/runtime contract for the optional durable incoming inbox
/// model. It records validated Bondstone receive deliveries and does not
/// execute handlers, settle broker messages, or infer operation state.
/// </remarks>
public interface IDurableIncomingInboxIngestionStore
{
    /// <summary>
    /// Inserts a durable incoming inbox record or returns the existing record for the same receive identity.
    /// </summary>
    /// <param name="record">The durable incoming inbox record to ingest.</param>
    /// <param name="ct">A cancellation token for the write operation.</param>
    /// <returns>The ingestion result and effective stored record.</returns>
    ValueTask<DurableIncomingInboxIngestionResult> IngestAsync(
        DurableIncomingInboxRecord record,
        CancellationToken ct = default);
}
