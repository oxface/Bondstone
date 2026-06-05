using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.EntityFrameworkCore.Operations;

public sealed class EntityFrameworkCoreDurableOperationStateStore<TDbContext>(
    TDbContext context)
    : IDurableOperationStateStore
    where TDbContext : DbContext
{
    public async ValueTask<DurableOperationState?> GetStateAsync(
        Guid durableOperationId,
        CancellationToken ct = default)
    {
        OperationStateEntity? entity = await context
            .Set<OperationStateEntity>()
            .FindAsync([durableOperationId], ct);

        return entity?.ToState();
    }

    public async ValueTask SaveAsync(
        DurableOperationState state,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        OperationStateEntity? existing = await context
            .Set<OperationStateEntity>()
            .FindAsync([state.DurableOperationId], ct);

        if (existing is null)
        {
            context.Set<OperationStateEntity>().Add(OperationStateEntity.FromState(state));
            return;
        }

        existing.ApplyState(state);
    }
}
