using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;

public sealed class PostgreSqlDurableOutboxClaimer<TDbContext>(
    TDbContext context,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableOutboxClaimer
    where TDbContext : DbContext
{
    private static readonly string MessageIdColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.MessageId);
    private static readonly string StatusColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.Status);
    private static readonly string AttemptCountColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.AttemptCount);
    private static readonly string NextAttemptAtUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.NextAttemptAtUtc);
    private static readonly string StoredAtUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.StoredAtUtc);
    private static readonly string ClaimedByColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.ClaimedBy);
    private static readonly string ClaimedUntilUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.ClaimedUntilUtc);

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly string _tableName = PostgreSqlOutboxTableIdentifier.BuildTableName(schema);

    public async ValueTask<IReadOnlyList<DurableOutboxRecord>> ClaimAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default)
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
                SELECT {{MessageIdColumn}}
                FROM {{_tableName}}
                WHERE (
                    {{StatusColumn}} = @pending
                    AND ({{NextAttemptAtUtcColumn}} IS NULL OR {{NextAttemptAtUtcColumn}} <= @nowUtc)
                )
                OR (
                    {{StatusColumn}} = @processing
                    AND {{ClaimedUntilUtcColumn}} IS NOT NULL
                    AND {{ClaimedUntilUtcColumn}} <= @nowUtc
                )
                ORDER BY {{StoredAtUtcColumn}}, {{MessageIdColumn}}
                FOR UPDATE SKIP LOCKED
                LIMIT @maxCount
            )
            UPDATE {{_tableName}} AS message
            SET
                {{StatusColumn}} = @processing,
                {{AttemptCountColumn}} = message.{{AttemptCountColumn}} + 1,
                {{ClaimedByColumn}} = @claimedBy,
                {{ClaimedUntilUtcColumn}} = @claimedUntilUtc
            FROM candidates
            WHERE message.{{MessageIdColumn}} = candidates.{{MessageIdColumn}}
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
            .ToListAsync(ct);

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
