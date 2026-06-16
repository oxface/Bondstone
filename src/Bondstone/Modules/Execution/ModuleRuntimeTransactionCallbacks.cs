namespace Bondstone.Modules;

internal sealed class ModuleRuntimeTransactionCallbacks
{
    private readonly List<Func<CancellationToken, ValueTask>> _committedCallbacks = [];
    private readonly List<Func<CancellationToken, ValueTask>> _rolledBackCallbacks = [];
    private int _observedTransactionDepth;

    public bool IsObserving => _observedTransactionDepth > 0;

    public IDisposable Observe()
    {
        _observedTransactionDepth++;
        return new ObservedTransactionScope(this);
    }

    public void OnCommitted(Func<CancellationToken, ValueTask> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (IsObserving)
        {
            _committedCallbacks.Add(callback);
        }
    }

    public void OnRolledBack(Func<CancellationToken, ValueTask> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (IsObserving)
        {
            _rolledBackCallbacks.Add(callback);
        }
    }

    public async ValueTask NotifyCommittedAsync(CancellationToken ct = default)
    {
        try
        {
            foreach (Func<CancellationToken, ValueTask> callback in _committedCallbacks)
            {
                await callback(ct);
            }
        }
        finally
        {
            Clear();
        }
    }

    public async ValueTask NotifyRolledBackAsync(CancellationToken ct = default)
    {
        try
        {
            foreach (Func<CancellationToken, ValueTask> callback in _rolledBackCallbacks)
            {
                await callback(ct);
            }
        }
        finally
        {
            Clear();
        }
    }

    private void EndObservation()
    {
        if (_observedTransactionDepth == 0)
        {
            throw new InvalidOperationException(
                "Observed module transaction scopes must be disposed only once.");
        }

        _observedTransactionDepth--;
        if (_observedTransactionDepth == 0)
        {
            Clear();
        }
    }

    private void Clear()
    {
        _committedCallbacks.Clear();
        _rolledBackCallbacks.Clear();
    }

    private sealed class ObservedTransactionScope(ModuleRuntimeTransactionCallbacks callbacks)
        : IDisposable
    {
        private ModuleRuntimeTransactionCallbacks? _callbacks = callbacks;

        public void Dispose()
        {
            ModuleRuntimeTransactionCallbacks? callbacks = _callbacks;
            if (callbacks is null)
            {
                return;
            }

            callbacks.EndObservation();
            _callbacks = null;
        }
    }
}
