using Bondstone.Persistence;
using Bondstone.Utility;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;

public sealed class EntityFrameworkCoreDurableIncomingInboxInspectionStore<TDbContext>(
    TDbContext context)
    : IDurableIncomingInboxInspectionStore
    where TDbContext : DbContext
{
    public async ValueTask<IReadOnlyList<DurableIncomingInboxRecord>> FindAsync(
        DurableIncomingInboxStatus? status = null,
        int maxCount = 100,
        DateTimeOffset? ingestedAtOrBeforeUtc = null,
        string? receiverModule = null,
        string? sourceTransportName = null,
        CancellationToken ct = default)
    {
        ValidateIncomingInboxMapping();
        ValidateMaxCount(maxCount);
        ValidateUtcTimestamp(ingestedAtOrBeforeUtc, nameof(ingestedAtOrBeforeUtc), "Ingested-at cutoff");

        string? normalizedReceiverModule = NormalizeOptionalFilter(
            receiverModule,
            nameof(receiverModule),
            "Receiver module");
        string? normalizedSourceTransportName = NormalizeOptionalFilter(
            sourceTransportName,
            nameof(sourceTransportName),
            "Source transport name");

        IQueryable<IncomingInboxMessageEntity> query = CreateBaseQuery(
            normalizedReceiverModule,
            normalizedSourceTransportName);

        if (status is not null)
        {
            query = query.Where(entity => entity.Status == status);
        }

        if (ingestedAtOrBeforeUtc is not null)
        {
            DateTimeOffset cutoff = ingestedAtOrBeforeUtc.Value;
            query = query.Where(entity => entity.IngestedAtUtc <= cutoff);
        }

        List<IncomingInboxMessageEntity> entities = await query
            .OrderBy(static entity => entity.IngestedAtUtc)
            .ThenBy(static entity => entity.ReceiverModule)
            .ThenBy(static entity => entity.MessageId)
            .ThenBy(static entity => entity.HandlerIdentity)
            .Take(maxCount)
            .ToListAsync(ct);

        return entities.Select(static entity => entity.ToRecord()).ToArray();
    }

    public async ValueTask<IReadOnlyList<DurableIncomingInboxRecord>> FindStaleProcessingAsync(
        DateTimeOffset claimedUntilAtOrBeforeUtc,
        int maxCount = 100,
        string? receiverModule = null,
        string? sourceTransportName = null,
        CancellationToken ct = default)
    {
        ValidateIncomingInboxMapping();
        ValidateMaxCount(maxCount);
        ValidateUtcTimestamp(
            claimedUntilAtOrBeforeUtc,
            nameof(claimedUntilAtOrBeforeUtc),
            "Claim lease expiration cutoff");

        string? normalizedReceiverModule = NormalizeOptionalFilter(
            receiverModule,
            nameof(receiverModule),
            "Receiver module");
        string? normalizedSourceTransportName = NormalizeOptionalFilter(
            sourceTransportName,
            nameof(sourceTransportName),
            "Source transport name");

        IQueryable<IncomingInboxMessageEntity> query = CreateBaseQuery(
                normalizedReceiverModule,
                normalizedSourceTransportName)
            .Where(entity =>
                entity.Status == DurableIncomingInboxStatus.Processing
                && entity.ClaimedUntilUtc != null
                && entity.ClaimedUntilUtc <= claimedUntilAtOrBeforeUtc);

        List<IncomingInboxMessageEntity> entities = await query
            .OrderBy(static entity => entity.ClaimedUntilUtc)
            .ThenBy(static entity => entity.IngestedAtUtc)
            .ThenBy(static entity => entity.ReceiverModule)
            .ThenBy(static entity => entity.MessageId)
            .ThenBy(static entity => entity.HandlerIdentity)
            .Take(maxCount)
            .ToListAsync(ct);

        return entities.Select(static entity => entity.ToRecord()).ToArray();
    }

    public async ValueTask<IReadOnlyList<DurableIncomingInboxRecord>> FindTerminalFailedAsync(
        int maxCount = 100,
        DateTimeOffset? failedAtOrBeforeUtc = null,
        string? receiverModule = null,
        string? sourceTransportName = null,
        CancellationToken ct = default)
    {
        ValidateIncomingInboxMapping();
        ValidateMaxCount(maxCount);
        ValidateUtcTimestamp(failedAtOrBeforeUtc, nameof(failedAtOrBeforeUtc), "Failed-at cutoff");

        string? normalizedReceiverModule = NormalizeOptionalFilter(
            receiverModule,
            nameof(receiverModule),
            "Receiver module");
        string? normalizedSourceTransportName = NormalizeOptionalFilter(
            sourceTransportName,
            nameof(sourceTransportName),
            "Source transport name");

        IQueryable<IncomingInboxMessageEntity> query = CreateBaseQuery(
                normalizedReceiverModule,
                normalizedSourceTransportName)
            .Where(static entity => entity.Status == DurableIncomingInboxStatus.TerminalFailed);

        if (failedAtOrBeforeUtc is not null)
        {
            DateTimeOffset cutoff = failedAtOrBeforeUtc.Value;
            query = query.Where(entity =>
                entity.FailedAtUtc != null
                && entity.FailedAtUtc <= cutoff);
        }

        List<IncomingInboxMessageEntity> entities = await query
            .OrderBy(static entity => entity.FailedAtUtc)
            .ThenBy(static entity => entity.IngestedAtUtc)
            .ThenBy(static entity => entity.ReceiverModule)
            .ThenBy(static entity => entity.MessageId)
            .ThenBy(static entity => entity.HandlerIdentity)
            .Take(maxCount)
            .ToListAsync(ct);

        return entities.Select(static entity => entity.ToRecord()).ToArray();
    }

    private IQueryable<IncomingInboxMessageEntity> CreateBaseQuery(
        string? receiverModule,
        string? sourceTransportName)
    {
        IQueryable<IncomingInboxMessageEntity> query = context.Set<IncomingInboxMessageEntity>()
            .AsNoTracking();

        if (receiverModule is not null)
        {
            query = query.Where(entity => entity.ReceiverModule == receiverModule);
        }

        if (sourceTransportName is not null)
        {
            query = query.Where(entity => entity.SourceTransportName == sourceTransportName);
        }

        return query;
    }

    private void ValidateIncomingInboxMapping()
    {
        if (context.Model.FindEntityType(typeof(IncomingInboxMessageEntity)) is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"DbContext '{context.GetType().FullName}' is missing the Bondstone EF Core incoming inbox mapping. Map the durable incoming inbox explicitly with ApplyBondstoneIncomingInbox().");
    }

    private static string? NormalizeOptionalFilter(
        string? value,
        string parameterName,
        string valueName)
    {
        return value is null
            ? null
            : value.NormalizeRequired(parameterName, valueName);
    }

    private static void ValidateMaxCount(int maxCount)
    {
        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCount),
                maxCount,
                "Maximum inspection count must be positive.");
        }
    }

    private static void ValidateUtcTimestamp(
        DateTimeOffset value,
        string parameterName,
        string valueName)
    {
        if (value == default)
        {
            throw new ArgumentException($"{valueName} must not be the default value.", parameterName);
        }

        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException($"{valueName} must use UTC offset.", parameterName);
        }
    }

    private static void ValidateUtcTimestamp(
        DateTimeOffset? value,
        string parameterName,
        string valueName)
    {
        if (value is null)
        {
            return;
        }

        ValidateUtcTimestamp(value.Value, parameterName, valueName);
    }
}
