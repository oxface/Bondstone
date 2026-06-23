using Bondstone.Persistence;
using Bondstone.Utility;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Inbox;

public sealed class EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>(
    TDbContext context)
    : IDurableInboxInspectionStore
    where TDbContext : DbContext
{
    public async ValueTask<IReadOnlyList<DurableInboxRecord>> FindUnprocessedAsync(
        int maxCount = 100,
        DateTimeOffset? receivedAtOrBeforeUtc = null,
        string? moduleName = null,
        CancellationToken ct = default)
    {
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCount),
                maxCount,
                "Maximum inspection count must be positive.");
        }

        if (receivedAtOrBeforeUtc is not null
            && receivedAtOrBeforeUtc.Value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException(
                "Received-at cutoff must use UTC offset.",
                nameof(receivedAtOrBeforeUtc));
        }

        string? normalizedModuleName = moduleName is null
            ? null
            : moduleName.NormalizeRequired(nameof(moduleName), "Module name");

        IQueryable<InboxMessageEntity> query = context.Set<InboxMessageEntity>()
            .AsNoTracking()
            .Where(static entity => entity.ProcessedAtUtc == null);

        if (normalizedModuleName is not null)
        {
            query = query.Where(entity => entity.ModuleName == normalizedModuleName);
        }

        if (receivedAtOrBeforeUtc is not null)
        {
            DateTimeOffset cutoff = receivedAtOrBeforeUtc.Value;
            query = query.Where(entity => entity.ReceivedAtUtc <= cutoff);
        }

        List<InboxMessageEntity> entities = await query
            .OrderBy(static entity => entity.ReceivedAtUtc)
            .ThenBy(static entity => entity.ModuleName)
            .ThenBy(static entity => entity.MessageId)
            .ThenBy(static entity => entity.HandlerIdentity)
            .Take(maxCount)
            .ToListAsync(ct);

        return entities.Select(static entity => entity.ToRecord()).ToArray();
    }
}
