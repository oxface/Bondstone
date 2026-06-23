namespace Bondstone.Persistence;

/// <summary>
/// Resolves the persistence boundary that owns durable incoming inbox ingestion for a receiver module.
/// </summary>
public interface IDurableIncomingInboxIngestionBoundaryResolver
{
    /// <summary>
    /// Resolves the ingestion store and commit scope for the receiver module.
    /// </summary>
    /// <param name="receiverModule">The module that owns the incoming inbox receive identity.</param>
    /// <returns>The ingestion boundary for the receiver module.</returns>
    DurableIncomingInboxIngestionBoundary Resolve(string receiverModule);
}
