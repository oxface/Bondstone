namespace Bondstone.Persistence;

/// <summary>
/// Coordinates optional system behavior with the current module transaction.
/// </summary>
/// <remarks>
/// This is an advanced provider/runtime contract. Callbacks are intended for
/// lightweight in-memory cleanup or runtime coordination after Bondstone has
/// observed transaction completion. They must not be used as a durable work
/// boundary because commit callbacks run after the transaction has already
/// committed, and callback failures can still surface to the caller.
/// </remarks>
public interface IModuleTransactionFeature
{
    /// <summary>
    /// Gets whether Bondstone observes the commit or rollback for this
    /// transaction boundary.
    /// </summary>
    bool ObservesCommit { get; }

    /// <summary>
    /// Registers a callback to run after Bondstone observes commit.
    /// </summary>
    void OnCommitted(Func<CancellationToken, ValueTask> callback);

    /// <summary>
    /// Registers a callback to run after Bondstone observes rollback.
    /// </summary>
    void OnRolledBack(Func<CancellationToken, ValueTask> callback);
}
