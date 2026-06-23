namespace Bondstone.Persistence;

/// <summary>
/// Provides the commit boundary for durable incoming inbox ingestion.
/// </summary>
/// <remarks>
/// Transport ingestion adapters use this provider/runtime contract to commit
/// staged incoming inbox rows before native broker settlement. The scope does
/// not execute handlers or record processing outcomes.
/// </remarks>
public interface IDurableIncomingInboxIngestionPersistenceScope
{
    /// <summary>
    /// Runs an ingestion operation inside the provider's persistence boundary.
    /// </summary>
    /// <typeparam name="TResult">The operation result type.</typeparam>
    /// <param name="operation">The ingestion operation to run.</param>
    /// <param name="ct">A cancellation token for the persistence operation.</param>
    /// <returns>The operation result after the persistence boundary completes.</returns>
    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IDurableIncomingInboxIngestionPersistenceScope, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Saves staged incoming inbox changes inside the active persistence boundary.
    /// </summary>
    /// <param name="ct">A cancellation token for the save operation.</param>
    ValueTask SaveChangesAsync(CancellationToken ct = default);
}
