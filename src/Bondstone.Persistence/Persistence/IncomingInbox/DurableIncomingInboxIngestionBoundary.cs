namespace Bondstone.Persistence;

/// <summary>
/// Groups the store and commit scope for one durable incoming inbox ingestion boundary.
/// </summary>
/// <remarks>
/// This provider/runtime contract is used by transport ingestion adapters after
/// resolving the receiver module for an incoming durable envelope.
/// </remarks>
public sealed class DurableIncomingInboxIngestionBoundary
{
    public DurableIncomingInboxIngestionBoundary(
        IDurableIncomingInboxIngestionStore store,
        IDurableIncomingInboxIngestionPersistenceScope persistenceScope)
    {
        Store = store ?? throw new ArgumentNullException(nameof(store));
        PersistenceScope = persistenceScope
            ?? throw new ArgumentNullException(nameof(persistenceScope));
    }

    public IDurableIncomingInboxIngestionStore Store { get; }

    public IDurableIncomingInboxIngestionPersistenceScope PersistenceScope { get; }

    /// <summary>
    /// Ingests a durable incoming inbox record and commits the boundary before returning.
    /// </summary>
    /// <param name="record">The durable incoming inbox record to ingest.</param>
    /// <param name="ct">A cancellation token for the persistence operation.</param>
    /// <returns>The ingestion result and effective stored record.</returns>
    public ValueTask<DurableIncomingInboxIngestionResult> IngestAndSaveAsync(
        DurableIncomingInboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        return PersistenceScope.ExecuteAsync(
            async (persistence, innerCt) =>
            {
                DurableIncomingInboxIngestionResult result =
                    await Store.IngestAsync(record, innerCt);
                await persistence.SaveChangesAsync(innerCt);
                return result;
            },
            ct);
    }
}
