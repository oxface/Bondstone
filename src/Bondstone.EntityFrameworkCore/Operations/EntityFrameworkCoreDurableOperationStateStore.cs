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
        ValidateOperationStateMapping();

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
        ValidateOperationStateMapping();

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

    private void ValidateOperationStateMapping()
    {
        if (context.Model.FindEntityType(typeof(OperationStateEntity)) is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"DbContext '{context.GetType().FullName}' is missing the Bondstone EF Core operation-state mapping. Map operation state with ApplyBondstoneOperationState(), or use ApplyBondstonePersistence().");
    }
}
