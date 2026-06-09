using Bondstone.Persistence;
using Bondstone.Persistence.Dapper.Postgres.Persistence;
using Dapper;
using Npgsql;

namespace Bondstone.Persistence.Dapper.Postgres.Outbox;

public sealed class PostgresDapperDurableOutboxClaimer(
    NpgsqlDataSource dataSource,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableOutboxClaimer
{
    private readonly NpgsqlDataSource _dataSource =
        dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly string _tableName = PostgresDapperTableIdentifier.Build(
        PostgresDapperDurableTableNames.OutboxMessages,
        schema);

    public async ValueTask<IReadOnlyList<DurableOutboxRecord>> ClaimAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default)
    {
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCount));
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        DateTimeOffset claimedUntilUtc = nowUtc.Add(leaseDuration);

        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(ct);
        IEnumerable<PostgresDapperOutboxRow> rows =
            await connection.QueryAsync<PostgresDapperOutboxRow>(new CommandDefinition(
                $$"""
                WITH candidates AS (
                    SELECT "MessageId"
                    FROM {{_tableName}}
                    WHERE (
                        "Status" = @Pending
                        AND ("NextAttemptAtUtc" IS NULL OR "NextAttemptAtUtc" <= @NowUtc)
                    )
                    OR (
                        "Status" = @Processing
                        AND "ClaimedUntilUtc" IS NOT NULL
                        AND "ClaimedUntilUtc" <= @NowUtc
                    )
                    ORDER BY "StoredAtUtc", "MessageId"
                    FOR UPDATE SKIP LOCKED
                    LIMIT @MaxCount
                )
                UPDATE {{_tableName}} AS message
                SET
                    "Status" = @Processing,
                    "AttemptCount" = message."AttemptCount" + 1,
                    "ClaimedBy" = @ClaimedBy,
                    "ClaimedUntilUtc" = @ClaimedUntilUtc
                FROM candidates
                WHERE message."MessageId" = candidates."MessageId"
                RETURNING message.*
                """,
                new
                {
                    Pending = DurableOutboxStatus.Pending.ToString(),
                    Processing = DurableOutboxStatus.Processing.ToString(),
                    NowUtc = nowUtc,
                    MaxCount = maxCount,
                    ClaimedBy = normalizedClaimedBy,
                    ClaimedUntilUtc = claimedUntilUtc,
                },
                cancellationToken: ct));

        return rows.Select(static row => row.ToRecord()).ToArray();
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
