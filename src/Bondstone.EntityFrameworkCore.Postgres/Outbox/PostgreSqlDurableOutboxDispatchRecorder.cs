using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bondstone.EntityFrameworkCore.Postgres.Outbox;

public sealed class PostgreSqlDurableOutboxDispatchRecorder<TDbContext>(
    TDbContext context,
    string? schema = null)
    : IDurableOutboxDispatchRecorder
    where TDbContext : DbContext
{
    private readonly string _tableName = PostgreSqlOutboxTableIdentifier.BuildTableName(schema);

    public async ValueTask<bool> MarkDispatchedAsync(
        Guid messageId,
        string claimedBy,
        DateTimeOffset dispatchedAtUtc,
        CancellationToken cancellationToken = default)
    {
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        ValidateUtcTimestamp(dispatchedAtUtc, nameof(dispatchedAtUtc), "Dispatched timestamp");

        string sql =
            $$"""
            UPDATE {{_tableName}}
            SET
                "Status" = @dispatched,
                "NextAttemptAtUtc" = NULL,
                "DispatchedAtUtc" = @dispatchedAtUtc,
                "FailedAtUtc" = NULL,
                "FailureReason" = NULL,
                "ClaimedBy" = NULL,
                "ClaimedUntilUtc" = NULL
            WHERE "MessageId" = @messageId
            AND "Status" = @processing
            AND "ClaimedBy" = @claimedBy
            AND "ClaimedUntilUtc" >= @dispatchedAtUtc
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
            cancellationToken);

        return rowCount == 1;
    }

    public async ValueTask<bool> ScheduleRetryAsync(
        Guid messageId,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset nextAttemptAtUtc,
        CancellationToken cancellationToken = default)
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
                "Status" = @pending,
                "NextAttemptAtUtc" = @nextAttemptAtUtc,
                "DispatchedAtUtc" = NULL,
                "FailedAtUtc" = @failedAtUtc,
                "FailureReason" = @failureReason,
                "ClaimedBy" = NULL,
                "ClaimedUntilUtc" = NULL
            WHERE "MessageId" = @messageId
            AND "Status" = @processing
            AND "ClaimedBy" = @claimedBy
            AND "ClaimedUntilUtc" >= @failedAtUtc
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
            cancellationToken);

        return rowCount == 1;
    }

    public async ValueTask<bool> MarkDeadLetteredAsync(
        Guid messageId,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        CancellationToken cancellationToken = default)
    {
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        string normalizedFailureReason = NormalizeFailureReason(failureReason);
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");

        string sql =
            $$"""
            UPDATE {{_tableName}}
            SET
                "Status" = @deadLettered,
                "NextAttemptAtUtc" = NULL,
                "DispatchedAtUtc" = NULL,
                "FailedAtUtc" = @failedAtUtc,
                "FailureReason" = @failureReason,
                "ClaimedBy" = NULL,
                "ClaimedUntilUtc" = NULL
            WHERE "MessageId" = @messageId
            AND "Status" = @processing
            AND "ClaimedBy" = @claimedBy
            AND "ClaimedUntilUtc" >= @failedAtUtc
            """;

        int rowCount = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("deadLettered", DurableOutboxStatus.DeadLettered.ToString()),
                new NpgsqlParameter("failedAtUtc", failedAtUtc),
                new NpgsqlParameter("failureReason", normalizedFailureReason),
                new NpgsqlParameter("messageId", messageId),
                new NpgsqlParameter("processing", DurableOutboxStatus.Processing.ToString()),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
            ],
            cancellationToken);

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
