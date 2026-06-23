using System.ComponentModel;

namespace Bondstone.Modules;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IModuleRuntimeExecutionContext
{
    /// <summary>
    /// Gets the module currently executing.
    /// </summary>
    string ModuleName { get; }

    /// <summary>
    /// Gets whether Bondstone observes the current module transaction outcome.
    /// </summary>
    bool ObservesTransactionOutcome { get; }

    /// <summary>
    /// Opens a scope where provider runtime services can register observed
    /// transaction callbacks.
    /// </summary>
    IDisposable ObserveTransactionOutcome();

    /// <summary>
    /// Registers a callback to run after Bondstone observes transaction commit.
    /// </summary>
    void OnTransactionCommitted(Func<CancellationToken, ValueTask> callback);

    /// <summary>
    /// Registers a callback to run after Bondstone observes transaction rollback.
    /// </summary>
    void OnTransactionRolledBack(Func<CancellationToken, ValueTask> callback);

    /// <summary>
    /// Runs callbacks registered for observed transaction commit.
    /// </summary>
    ValueTask NotifyTransactionCommittedAsync(CancellationToken ct = default);

    /// <summary>
    /// Runs callbacks registered for observed transaction rollback.
    /// </summary>
    ValueTask NotifyTransactionRolledBackAsync(CancellationToken ct = default);
}
