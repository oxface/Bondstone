using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.Persistence;

internal sealed class DurableModuleOperationReader : IDurableOperationReader
{
    private readonly ModuleRuntimeRegistry _moduleRuntimeRegistry;

    public DurableModuleOperationReader(ModuleRuntimeRegistry moduleRuntimeRegistry)
    {
        _moduleRuntimeRegistry =
            moduleRuntimeRegistry ?? throw new ArgumentNullException(nameof(moduleRuntimeRegistry));
        _moduleRuntimeRegistry.ValidateDurableOperationStateStores();
    }

    public async ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        CancellationToken ct = default)
    {
        if (!_moduleRuntimeRegistry.HasDurableModulePersistenceRegistrations)
        {
            return null;
        }

        DurableOperationState? bestState = null;
        foreach (IDurableOperationStateStore store in _moduleRuntimeRegistry
            .CreateDurableOperationStateStores())
        {
            DurableOperationState? state = await store.GetStateAsync(
                durableOperationId,
                ct);

            if (state is null)
            {
                continue;
            }

            if (bestState is null || Compare(state, bestState) > 0)
            {
                bestState = state;
            }
        }

        return bestState;
    }

    private static int Compare(
        DurableOperationState left,
        DurableOperationState right)
    {
        int rankComparison = GetStatusRank(left.Status).CompareTo(
            GetStatusRank(right.Status));

        if (rankComparison != 0)
        {
            return rankComparison;
        }

        return left.UpdatedAtUtc.CompareTo(right.UpdatedAtUtc);
    }

    private static int GetStatusRank(DurableOperationStatus status)
    {
        return status switch
        {
            DurableOperationStatus.Pending => 1,
            DurableOperationStatus.Running => 2,
            DurableOperationStatus.Completed => 3,
            DurableOperationStatus.Failed => 3,
            DurableOperationStatus.Cancelled => 3,
            _ => 0,
        };
    }
}
