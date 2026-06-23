using Bondstone.Messaging;

namespace Bondstone.Persistence;

/// <summary>
/// Finds non-terminal durable operations that application expiry policy may finalize.
/// </summary>
public interface IDurableOperationExpirationStore
{
    /// <summary>
    /// Finds pending or running operations last updated at or before the supplied UTC cutoff.
    /// </summary>
    /// <param name="expiresBeforeUtc">The UTC cutoff for candidate operation-state update timestamps.</param>
    /// <param name="maxCount">The maximum number of candidates to return.</param>
    /// <param name="ct">A cancellation token for the query.</param>
    /// <returns>Operation states that may be finalized by application expiry policy.</returns>
    ValueTask<IReadOnlyList<DurableOperationState>> FindExpirationCandidatesAsync(
        DateTimeOffset expiresBeforeUtc,
        int maxCount,
        CancellationToken ct = default);
}
