namespace Bondstone.Modules;

internal abstract class ModuleRuntimeExecutionContextBase
    : IModuleRuntimeExecutionContext
{
    private readonly ModuleRuntimeTransactionCallbacks _transactionCallbacks = new();

    public abstract string ModuleName { get; }

    public bool ObservesTransactionOutcome => _transactionCallbacks.IsObserving;

    public IDisposable ObserveTransactionOutcome() => _transactionCallbacks.Observe();

    public void OnTransactionCommitted(Func<CancellationToken, ValueTask> callback) =>
        _transactionCallbacks.OnCommitted(callback);

    public void OnTransactionRolledBack(Func<CancellationToken, ValueTask> callback) =>
        _transactionCallbacks.OnRolledBack(callback);

    public ValueTask NotifyTransactionCommittedAsync(CancellationToken ct = default) =>
        _transactionCallbacks.NotifyCommittedAsync(ct);

    public ValueTask NotifyTransactionRolledBackAsync(CancellationToken ct = default) =>
        _transactionCallbacks.NotifyRolledBackAsync(ct);
}
