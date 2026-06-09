using Bondstone.Persistence;
using Bondstone.Persistence.Dapper.Postgres.Persistence;
using Dapper;
using Npgsql;

namespace Bondstone.Persistence.Dapper.Postgres.Outbox;

public sealed class PostgresDapperDurableOutboxDispatchRecorder(
    NpgsqlDataSource dataSource,
    string? schema = null)
    : IDurableOutboxDispatchRecorder
{
    private readonly NpgsqlDataSource _dataSource =
        dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    private readonly string _tableName = PostgresDapperTableIdentifier.Build(
        PostgresDapperDurableTableNames.OutboxMessages,
        schema);

    public ValueTask<bool> MarkDispatchedAsync(
        Guid messageId,
        string claimedBy,
        DateTimeOffset dispatchedAtUtc,
        CancellationToken ct = default)
    {
        ValidateUtcTimestamp(
            dispatchedAtUtc,
            nameof(dispatchedAtUtc),
            "Dispatched timestamp");

        return UpdateClaimedAsync(
            """
            "Status" = @Dispatched,
            "NextAttemptAtUtc" = NULL,
            "DispatchedAtUtc" = @DispatchedAtUtc,
            "FailedAtUtc" = NULL,
            "FailureReason" = NULL,
            "ClaimedBy" = NULL,
            "ClaimedUntilUtc" = NULL
            """,
            messageId,
            claimedBy,
            dispatchedAtUtc,
            new
            {
                Dispatched = DurableOutboxStatus.Dispatched.ToString(),
                DispatchedAtUtc = dispatchedAtUtc,
            },
            ct);
    }

    public ValueTask<bool> ScheduleRetryAsync(
        Guid messageId,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset nextAttemptAtUtc,
        CancellationToken ct = default)
    {
        ValidateUtcTimestamp(
            failedAtUtc,
            nameof(failedAtUtc),
            "Failed timestamp");
        ValidateUtcTimestamp(
            nextAttemptAtUtc,
            nameof(nextAttemptAtUtc),
            "Next-attempt timestamp");

        if (nextAttemptAtUtc < failedAtUtc)
        {
            throw new ArgumentException(
                "Next-attempt timestamp must not be earlier than failed timestamp.",
                nameof(nextAttemptAtUtc));
        }

        return UpdateClaimedAsync(
            """
            "Status" = @Pending,
            "NextAttemptAtUtc" = @NextAttemptAtUtc,
            "DispatchedAtUtc" = NULL,
            "FailedAtUtc" = @FailedAtUtc,
            "FailureReason" = @FailureReason,
            "ClaimedBy" = NULL,
            "ClaimedUntilUtc" = NULL
            """,
            messageId,
            claimedBy,
            failedAtUtc,
            new
            {
                Pending = DurableOutboxStatus.Pending.ToString(),
                NextAttemptAtUtc = nextAttemptAtUtc,
                FailedAtUtc = failedAtUtc,
                FailureReason = NormalizeFailureReason(failureReason),
            },
            ct);
    }

    public ValueTask<bool> MarkDeadLetteredAsync(
        Guid messageId,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        CancellationToken ct = default)
    {
        ValidateUtcTimestamp(
            failedAtUtc,
            nameof(failedAtUtc),
            "Failed timestamp");

        return UpdateClaimedAsync(
            """
            "Status" = @DeadLettered,
            "NextAttemptAtUtc" = NULL,
            "DispatchedAtUtc" = NULL,
            "FailedAtUtc" = @FailedAtUtc,
            "FailureReason" = @FailureReason,
            "ClaimedBy" = NULL,
            "ClaimedUntilUtc" = NULL
            """,
            messageId,
            claimedBy,
            failedAtUtc,
            new
            {
                DeadLettered = DurableOutboxStatus.DeadLettered.ToString(),
                FailedAtUtc = failedAtUtc,
                FailureReason = NormalizeFailureReason(failureReason),
            },
            ct);
    }

    private async ValueTask<bool> UpdateClaimedAsync(
        string setSql,
        Guid messageId,
        string claimedBy,
        DateTimeOffset timestampUtc,
        object values,
        CancellationToken ct)
    {
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        var parameters = new DynamicParameters(values);
        parameters.Add("MessageId", messageId);
        parameters.Add("Processing", DurableOutboxStatus.Processing.ToString());
        parameters.Add("ClaimedBy", normalizedClaimedBy);
        parameters.Add("TimestampUtc", timestampUtc);

        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(ct);
        int rowCount = await connection.ExecuteAsync(new CommandDefinition(
            $$"""
            UPDATE {{_tableName}}
            SET {{setSql}}
            WHERE "MessageId" = @MessageId
            AND "Status" = @Processing
            AND "ClaimedBy" = @ClaimedBy
            AND "ClaimedUntilUtc" >= @TimestampUtc
            """,
            parameters,
            cancellationToken: ct));

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
