using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bondstone.EntityFrameworkCore.Postgres.Outbox;

public sealed class PostgreSqlDurableOutboxClaimer<TDbContext>(
    TDbContext context,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableOutboxClaimer
    where TDbContext : DbContext
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly string _tableName = PostgreSqlOutboxTableIdentifier.BuildTableName(schema);

    public async ValueTask<IReadOnlyList<DurableOutboxRecord>> ClaimAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration),
                leaseDuration,
                "Lease duration must be positive.");
        }

        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCount),
                maxCount,
                "Maximum claim count must be positive.");
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        DateTimeOffset claimedUntilUtc = nowUtc.Add(leaseDuration);
        string pending = DurableOutboxStatus.Pending.ToString();
        string processing = DurableOutboxStatus.Processing.ToString();

        string sql =
            $$"""
            WITH candidates AS (
                SELECT "MessageId"
                FROM {{_tableName}}
                WHERE (
                    "Status" = @pending
                    AND ("NextAttemptAtUtc" IS NULL OR "NextAttemptAtUtc" <= @nowUtc)
                )
                OR (
                    "Status" = @processing
                    AND "ClaimedUntilUtc" IS NOT NULL
                    AND "ClaimedUntilUtc" <= @nowUtc
                )
                ORDER BY "StoredAtUtc", "MessageId"
                FOR UPDATE SKIP LOCKED
                LIMIT @maxCount
            )
            UPDATE {{_tableName}} AS message
            SET
                "Status" = @processing,
                "AttemptCount" = message."AttemptCount" + 1,
                "ClaimedBy" = @claimedBy,
                "ClaimedUntilUtc" = @claimedUntilUtc
            FROM candidates
            WHERE message."MessageId" = candidates."MessageId"
            RETURNING message.*
            """;

        List<OutboxMessageEntity> entities = await context
            .Set<OutboxMessageEntity>()
            .FromSqlRaw(
                sql,
                new NpgsqlParameter("pending", pending),
                new NpgsqlParameter("processing", processing),
                new NpgsqlParameter("nowUtc", nowUtc),
                new NpgsqlParameter("maxCount", maxCount),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
                new NpgsqlParameter("claimedUntilUtc", claimedUntilUtc))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities
            .Select(static entity => entity.ToRecord())
            .ToArray();
    }

    private static string NormalizeClaimedBy(string claimedBy)
    {
        if (string.IsNullOrWhiteSpace(claimedBy))
        {
            throw new ArgumentException("Claim owner is required.", nameof(claimedBy));
        }

        return claimedBy.Trim();
    }
}
