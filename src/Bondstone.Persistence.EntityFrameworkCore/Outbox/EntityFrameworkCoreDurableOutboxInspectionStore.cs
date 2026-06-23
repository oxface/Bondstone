using Bondstone.Persistence;
using Bondstone.Utility;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Outbox;

public sealed class EntityFrameworkCoreDurableOutboxInspectionStore<TDbContext>(
    TDbContext context)
    : IDurableOutboxInspectionStore
    where TDbContext : DbContext
{
    public async ValueTask<IReadOnlyList<DurableOutboxRecord>> FindTerminalFailedAsync(
        int maxCount = 100,
        DateTimeOffset? failedAtOrBeforeUtc = null,
        string? sourceModuleName = null,
        CancellationToken ct = default)
    {
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCount),
                maxCount,
                "Maximum inspection count must be positive.");
        }

        if (failedAtOrBeforeUtc is not null
            && failedAtOrBeforeUtc.Value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Failed-at cutoff must use UTC offset.",
                nameof(failedAtOrBeforeUtc));
        }

        string? normalizedSourceModuleName = sourceModuleName is null
            ? null
            : sourceModuleName.NormalizeRequired(nameof(sourceModuleName), "Source module");

        IQueryable<OutboxMessageEntity> query = context.Set<OutboxMessageEntity>()
            .AsNoTracking()
            .Where(static entity => entity.Status == DurableOutboxStatus.TerminalFailed);

        if (normalizedSourceModuleName is not null)
        {
            query = query.Where(entity => entity.SourceModule == normalizedSourceModuleName);
        }

        if (failedAtOrBeforeUtc is not null)
        {
            DateTimeOffset cutoff = failedAtOrBeforeUtc.Value;
            query = query.Where(entity =>
                (entity.FailedAtUtc ?? entity.StoredAtUtc) <= cutoff);
        }

        List<OutboxMessageEntity> entities = await query
            .OrderBy(static entity => entity.FailedAtUtc ?? entity.StoredAtUtc)
            .ThenBy(static entity => entity.MessageId)
            .Take(maxCount)
            .ToListAsync(ct);

        return entities.Select(static entity => entity.ToRecord()).ToArray();
    }
}
