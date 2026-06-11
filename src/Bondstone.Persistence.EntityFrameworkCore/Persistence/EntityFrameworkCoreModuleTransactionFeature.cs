using Bondstone.Persistence;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

internal sealed class EntityFrameworkCoreModuleTransactionFeature(bool observesCommit)
    : IModuleTransactionFeature
{
    private readonly List<Func<CancellationToken, ValueTask>> _committedCallbacks = [];
    private readonly List<Func<CancellationToken, ValueTask>> _rolledBackCallbacks = [];

    public bool ObservesCommit { get; } = observesCommit;

    public void OnCommitted(Func<CancellationToken, ValueTask> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (ObservesCommit)
        {
            _committedCallbacks.Add(callback);
        }
    }

    public void OnRolledBack(Func<CancellationToken, ValueTask> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (ObservesCommit)
        {
            _rolledBackCallbacks.Add(callback);
        }
    }

    public async ValueTask CommittedAsync(CancellationToken ct)
    {
        foreach (Func<CancellationToken, ValueTask> callback in _committedCallbacks)
        {
            await callback(ct);
        }
    }

    public async ValueTask RolledBackAsync(CancellationToken ct)
    {
        foreach (Func<CancellationToken, ValueTask> callback in _rolledBackCallbacks)
        {
            await callback(ct);
        }
    }
}
