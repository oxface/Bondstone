using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;

internal sealed class PostgreSqlDurableOutboxDispatchRecorder<TDbContext>(
    TDbContext context,
    string? schema = null)
    : IDurableOutboxDispatchRecorder
    where TDbContext : DbContext
{
    private static readonly string MessageIdColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.MessageId);
    private static readonly string StatusColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.Status);
    private static readonly string NextAttemptAtUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.NextAttemptAtUtc);
    private static readonly string DispatchedAtUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.DispatchedAtUtc);
    private static readonly string FailedAtUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.FailedAtUtc);
    private static readonly string FailureReasonColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.FailureReason);
    private static readonly string ClaimedByColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.ClaimedBy);
    private static readonly string ClaimedUntilUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.ClaimedUntilUtc);

    private readonly string _tableName = PostgreSqlOutboxTableIdentifier.BuildTableName(schema);

    public async ValueTask<bool> MarkDispatchedAsync(
        Guid messageId,
        string claimedBy,
        DateTimeOffset dispatchedAtUtc,
        CancellationToken ct = default)
    {
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        ValidateUtcTimestamp(dispatchedAtUtc, nameof(dispatchedAtUtc), "Dispatched timestamp");

        string sql =
            $$"""
            UPDATE {{_tableName}}
            SET
                {{StatusColumn}} = @dispatched,
                {{NextAttemptAtUtcColumn}} = NULL,
                {{DispatchedAtUtcColumn}} = @dispatchedAtUtc,
                {{FailedAtUtcColumn}} = NULL,
                {{FailureReasonColumn}} = NULL,
                {{ClaimedByColumn}} = NULL,
                {{ClaimedUntilUtcColumn}} = NULL
            WHERE {{MessageIdColumn}} = @messageId
            AND {{StatusColumn}} = @processing
            AND {{ClaimedByColumn}} = @claimedBy
            AND {{ClaimedUntilUtcColumn}} >= @dispatchedAtUtc
            """;

        int rowCount = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("dispatched", DurableOutboxStatus.Dispatched.ToString()),
                new NpgsqlParameter("dispatchedAtUtc", dispatchedAtUtc),
                new NpgsqlParameter("messageId", messageId),
                new NpgsqlParameter("processing", DurableOutboxStatus.Processing.ToString()),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
            ],
            ct);

        return rowCount == 1;
    }

    public async ValueTask<bool> ScheduleRetryAsync(
        Guid messageId,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset nextAttemptAtUtc,
        CancellationToken ct = default)
    {
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        string normalizedFailureReason = NormalizeFailureReason(failureReason);
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");
        ValidateUtcTimestamp(nextAttemptAtUtc, nameof(nextAttemptAtUtc), "Next-attempt timestamp");

        if (nextAttemptAtUtc < failedAtUtc)
        {
            throw new ArgumentException(
                "Next-attempt timestamp must not be earlier than failed timestamp.",
                nameof(nextAttemptAtUtc));
        }

        string sql =
            $$"""
            UPDATE {{_tableName}}
            SET
                {{StatusColumn}} = @pending,
                {{NextAttemptAtUtcColumn}} = @nextAttemptAtUtc,
                {{DispatchedAtUtcColumn}} = NULL,
                {{FailedAtUtcColumn}} = @failedAtUtc,
                {{FailureReasonColumn}} = @failureReason,
                {{ClaimedByColumn}} = NULL,
                {{ClaimedUntilUtcColumn}} = NULL
            WHERE {{MessageIdColumn}} = @messageId
            AND {{StatusColumn}} = @processing
            AND {{ClaimedByColumn}} = @claimedBy
            AND {{ClaimedUntilUtcColumn}} >= @failedAtUtc
            """;

        int rowCount = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("pending", DurableOutboxStatus.Pending.ToString()),
                new NpgsqlParameter("nextAttemptAtUtc", nextAttemptAtUtc),
                new NpgsqlParameter("failedAtUtc", failedAtUtc),
                new NpgsqlParameter("failureReason", normalizedFailureReason),
                new NpgsqlParameter("messageId", messageId),
                new NpgsqlParameter("processing", DurableOutboxStatus.Processing.ToString()),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
            ],
            ct);

        return rowCount == 1;
    }

    public async ValueTask<bool> MarkTerminalFailedAsync(
        Guid messageId,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        CancellationToken ct = default)
    {
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        string normalizedFailureReason = NormalizeFailureReason(failureReason);
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");

        string sql =
            $$"""
            UPDATE {{_tableName}}
            SET
                {{StatusColumn}} = @terminalFailed,
                {{NextAttemptAtUtcColumn}} = NULL,
                {{DispatchedAtUtcColumn}} = NULL,
                {{FailedAtUtcColumn}} = @failedAtUtc,
                {{FailureReasonColumn}} = @failureReason,
                {{ClaimedByColumn}} = NULL,
                {{ClaimedUntilUtcColumn}} = NULL
            WHERE {{MessageIdColumn}} = @messageId
            AND {{StatusColumn}} = @processing
            AND {{ClaimedByColumn}} = @claimedBy
            AND {{ClaimedUntilUtcColumn}} >= @failedAtUtc
            """;

        int rowCount = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("terminalFailed", DurableOutboxStatus.TerminalFailed.ToString()),
                new NpgsqlParameter("failedAtUtc", failedAtUtc),
                new NpgsqlParameter("failureReason", normalizedFailureReason),
                new NpgsqlParameter("messageId", messageId),
                new NpgsqlParameter("processing", DurableOutboxStatus.Processing.ToString()),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
            ],
            ct);

        return rowCount == 1;
    }

    private static string NormalizeClaimedBy(string claimedBy)
    {
        if (string.IsNullOrWhiteSpace(claimedBy))
        {
            throw new ArgumentException("Claim owner is required.", nameof(claimedBy));
        }

        return claimedBy.Trim();
    }

    private static string NormalizeFailureReason(string failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException("Failure reason is required.", nameof(failureReason));
        }

        return failureReason.Trim();
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
}
