using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.IncomingInbox;

internal sealed class PostgreSqlDurableIncomingInboxClaimer<TDbContext>(
    TDbContext context,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableIncomingInboxClaimer
    where TDbContext : DbContext
{
    private static readonly string MessageIdColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.MessageId);
    private static readonly string ReceiverModuleColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.ReceiverModule);
    private static readonly string HandlerIdentityColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.HandlerIdentity);
    private static readonly string StatusColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.Status);
    private static readonly string AttemptCountColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.AttemptCount);
    private static readonly string NextAttemptAtUtcColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.NextAttemptAtUtc);
    private static readonly string ProcessedAtUtcColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.ProcessedAtUtc);
    private static readonly string FailedAtUtcColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.FailedAtUtc);
    private static readonly string FailureReasonColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.FailureReason);
    private static readonly string ClaimedByColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.ClaimedBy);
    private static readonly string ClaimedUntilUtcColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.ClaimedUntilUtc);
    private static readonly string IngestedAtUtcColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.IngestedAtUtc);

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly string _tableName = PostgreSqlIncomingInboxTableIdentifier.BuildTableName(schema);

    public async ValueTask<IReadOnlyList<DurableIncomingInboxRecord>> ClaimAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default)
    {
        ValidateIncomingInboxMapping();
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
        string pending = DurableIncomingInboxStatus.Pending.ToString();
        string retryScheduled = DurableIncomingInboxStatus.RetryScheduled.ToString();
        string processing = DurableIncomingInboxStatus.Processing.ToString();

        string sql =
            $$"""
            WITH candidates AS (
                SELECT
                    {{ReceiverModuleColumn}},
                    {{MessageIdColumn}},
                    {{HandlerIdentityColumn}},
                    {{IngestedAtUtcColumn}}
                FROM {{_tableName}}
                WHERE {{StatusColumn}} = @pending
                OR (
                    {{StatusColumn}} = @retryScheduled
                    AND {{NextAttemptAtUtcColumn}} IS NOT NULL
                    AND {{NextAttemptAtUtcColumn}} <= @nowUtc
                )
                OR (
                    {{StatusColumn}} = @processing
                    AND {{ClaimedUntilUtcColumn}} IS NOT NULL
                    AND {{ClaimedUntilUtcColumn}} <= @nowUtc
                )
                ORDER BY
                    {{IngestedAtUtcColumn}},
                    {{MessageIdColumn}},
                    {{ReceiverModuleColumn}},
                    {{HandlerIdentityColumn}}
                FOR UPDATE SKIP LOCKED
                LIMIT @maxCount
            ),
            updated AS (
                UPDATE {{_tableName}} AS message
                SET
                    {{StatusColumn}} = @processing,
                    {{AttemptCountColumn}} = message.{{AttemptCountColumn}} + 1,
                    {{NextAttemptAtUtcColumn}} = NULL,
                    {{ProcessedAtUtcColumn}} = NULL,
                    {{FailedAtUtcColumn}} = NULL,
                    {{FailureReasonColumn}} = NULL,
                    {{ClaimedByColumn}} = @claimedBy,
                    {{ClaimedUntilUtcColumn}} = @claimedUntilUtc
                FROM candidates
                WHERE message.{{ReceiverModuleColumn}} = candidates.{{ReceiverModuleColumn}}
                AND message.{{MessageIdColumn}} = candidates.{{MessageIdColumn}}
                AND message.{{HandlerIdentityColumn}} = candidates.{{HandlerIdentityColumn}}
                RETURNING message.*
            )
            SELECT *
            FROM updated
            ORDER BY
                {{IngestedAtUtcColumn}},
                {{MessageIdColumn}},
                {{ReceiverModuleColumn}},
                {{HandlerIdentityColumn}}
            """;

        List<IncomingInboxMessageEntity> entities = await context
            .Set<IncomingInboxMessageEntity>()
            .FromSqlRaw(
                sql,
                new NpgsqlParameter("pending", pending),
                new NpgsqlParameter("retryScheduled", retryScheduled),
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

    private void ValidateIncomingInboxMapping()
    {
        if (context.Model.FindEntityType(typeof(IncomingInboxMessageEntity)) is not null)
        {
            return;
        }

        throw new InvalidOperationException(
            $"DbContext '{context.GetType().FullName}' is missing the Bondstone EF Core incoming inbox mapping. Map the durable incoming inbox explicitly with ApplyBondstoneIncomingInbox().");
    }

    private static string NormalizeClaimedBy(string claimedBy)
    {
        if (string.IsNullOrWhiteSpace(claimedBy))
        {
            throw new ArgumentException("Claim owner is required.", nameof(claimedBy));
        }

        return claimedBy.Trim();
    }

    private static string Quote(string value)
    {
        return PostgreSqlTableIdentifier.QuoteIdentifier(value);
    }
}
