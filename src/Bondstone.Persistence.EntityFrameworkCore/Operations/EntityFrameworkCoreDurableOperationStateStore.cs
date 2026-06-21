using Bondstone.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Operations;

public sealed class EntityFrameworkCoreDurableOperationStateStore<TDbContext>(
    TDbContext context)
    : IDurableOperationStateStore,
        IDurableOperationExpirationStore
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

    public async ValueTask<IReadOnlyList<DurableOperationState>> FindExpirationCandidatesAsync(
        DateTimeOffset expiresBeforeUtc,
        int maxCount,
        CancellationToken ct = default)
    {
        ValidateOperationStateMapping();
        ValidateCutoff(expiresBeforeUtc);
        ValidateMaxCount(maxCount);

        List<OperationStateEntity> entities = await context
            .Set<OperationStateEntity>()
            .AsNoTracking()
            .Where(static entity =>
                entity.Status == DurableOperationStatus.Pending
                || entity.Status == DurableOperationStatus.Running)
            .Where(entity => entity.UpdatedAtUtc <= expiresBeforeUtc)
            .OrderBy(static entity => entity.UpdatedAtUtc)
            .Take(maxCount)
            .ToListAsync(ct);

        return entities
            .Select(static entity => entity.ToState())
            .ToArray();
    }

    private void ValidateOperationStateMapping()
    {
        if (context.Model.FindEntityType(typeof(OperationStateEntity)) is not null)
        {
            return;
        }

        throw new BondstoneSetupException(
            BondstoneSetupCodes.MissingEfMapping,
            $"DbContext '{context.GetType().FullName}' is missing the Bondstone EF Core operation-state mapping. Map operation state with ApplyBondstoneOperationState(), or use ApplyBondstonePersistence().");
    }

    private static void ValidateCutoff(DateTimeOffset expiresBeforeUtc)
    {
        if (expiresBeforeUtc == default)
        {
            throw new ArgumentException("Expiry cutoff must not be the default value.", nameof(expiresBeforeUtc));
        }

        if (expiresBeforeUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Expiry cutoff must use UTC offset.", nameof(expiresBeforeUtc));
        }
    }

    private static void ValidateMaxCount(int maxCount)
    {
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCount),
                maxCount,
                "Maximum expiry count must be greater than zero.");
        }
    }
}
