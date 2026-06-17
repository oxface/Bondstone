using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.IncomingInbox;

internal sealed class PostgreSqlDurableIncomingInboxOutcomeRecorder<TDbContext>(
    TDbContext context,
    string? schema = null)
    : IDurableIncomingInboxOutcomeRecorder
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

    private readonly string _tableName = PostgreSqlIncomingInboxTableIdentifier.BuildTableName(schema);

    public async ValueTask<bool> MarkProcessedAsync(
        DurableIncomingInboxKey key,
        string claimedBy,
        DateTimeOffset processedAtUtc,
        CancellationToken ct = default)
    {
        ValidateIncomingInboxMapping();
        ArgumentNullException.ThrowIfNull(key);
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        ValidateUtcTimestamp(processedAtUtc, nameof(processedAtUtc), "Processed timestamp");

        string sql =
            $$"""
            UPDATE {{_tableName}}
            SET
                {{StatusColumn}} = @processed,
                {{NextAttemptAtUtcColumn}} = NULL,
                {{ProcessedAtUtcColumn}} = @processedAtUtc,
                {{FailedAtUtcColumn}} = NULL,
                {{FailureReasonColumn}} = NULL,
                {{ClaimedByColumn}} = NULL,
                {{ClaimedUntilUtcColumn}} = NULL
            WHERE {{ReceiverModuleColumn}} = @receiverModule
            AND {{MessageIdColumn}} = @messageId
            AND {{HandlerIdentityColumn}} = @handlerIdentity
            AND {{StatusColumn}} = @processing
            AND {{ClaimedByColumn}} = @claimedBy
            AND {{ClaimedUntilUtcColumn}} > @processedAtUtc
            """;

        int rowCount = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("processed", DurableIncomingInboxStatus.Processed.ToString()),
                new NpgsqlParameter("processedAtUtc", processedAtUtc),
                new NpgsqlParameter("receiverModule", key.ReceiverModule),
                new NpgsqlParameter("messageId", key.MessageId),
                new NpgsqlParameter("handlerIdentity", key.HandlerIdentity),
                new NpgsqlParameter("processing", DurableIncomingInboxStatus.Processing.ToString()),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
            ],
            ct);

        return rowCount == 1;
    }

    public async ValueTask<bool> ScheduleRetryAsync(
        DurableIncomingInboxKey key,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset nextAttemptAtUtc,
        CancellationToken ct = default)
    {
        ValidateIncomingInboxMapping();
        ArgumentNullException.ThrowIfNull(key);
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
                {{StatusColumn}} = @retryScheduled,
                {{NextAttemptAtUtcColumn}} = @nextAttemptAtUtc,
                {{ProcessedAtUtcColumn}} = NULL,
                {{FailedAtUtcColumn}} = @failedAtUtc,
                {{FailureReasonColumn}} = @failureReason,
                {{ClaimedByColumn}} = NULL,
                {{ClaimedUntilUtcColumn}} = NULL
            WHERE {{ReceiverModuleColumn}} = @receiverModule
            AND {{MessageIdColumn}} = @messageId
            AND {{HandlerIdentityColumn}} = @handlerIdentity
            AND {{StatusColumn}} = @processing
            AND {{ClaimedByColumn}} = @claimedBy
            AND {{ClaimedUntilUtcColumn}} > @failedAtUtc
            """;

        int rowCount = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("retryScheduled", DurableIncomingInboxStatus.RetryScheduled.ToString()),
                new NpgsqlParameter("nextAttemptAtUtc", nextAttemptAtUtc),
                new NpgsqlParameter("failedAtUtc", failedAtUtc),
                new NpgsqlParameter("failureReason", normalizedFailureReason),
                new NpgsqlParameter("receiverModule", key.ReceiverModule),
                new NpgsqlParameter("messageId", key.MessageId),
                new NpgsqlParameter("handlerIdentity", key.HandlerIdentity),
                new NpgsqlParameter("processing", DurableIncomingInboxStatus.Processing.ToString()),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
            ],
            ct);

        return rowCount == 1;
    }

    public async ValueTask<bool> MarkTerminalFailedAsync(
        DurableIncomingInboxKey key,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        CancellationToken ct = default)
    {
        ValidateIncomingInboxMapping();
        ArgumentNullException.ThrowIfNull(key);
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        string normalizedFailureReason = NormalizeFailureReason(failureReason);
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");

        string sql =
            $$"""
            UPDATE {{_tableName}}
            SET
                {{StatusColumn}} = @terminalFailed,
                {{NextAttemptAtUtcColumn}} = NULL,
                {{ProcessedAtUtcColumn}} = NULL,
                {{FailedAtUtcColumn}} = @failedAtUtc,
                {{FailureReasonColumn}} = @failureReason,
                {{ClaimedByColumn}} = NULL,
                {{ClaimedUntilUtcColumn}} = NULL
            WHERE {{ReceiverModuleColumn}} = @receiverModule
            AND {{MessageIdColumn}} = @messageId
            AND {{HandlerIdentityColumn}} = @handlerIdentity
            AND {{StatusColumn}} = @processing
            AND {{ClaimedByColumn}} = @claimedBy
            AND {{ClaimedUntilUtcColumn}} > @failedAtUtc
            """;

        int rowCount = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("terminalFailed", DurableIncomingInboxStatus.TerminalFailed.ToString()),
                new NpgsqlParameter("failedAtUtc", failedAtUtc),
                new NpgsqlParameter("failureReason", normalizedFailureReason),
                new NpgsqlParameter("receiverModule", key.ReceiverModule),
                new NpgsqlParameter("messageId", key.MessageId),
                new NpgsqlParameter("handlerIdentity", key.HandlerIdentity),
                new NpgsqlParameter("processing", DurableIncomingInboxStatus.Processing.ToString()),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
            ],
            ct);

        return rowCount == 1;
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

    private static string Quote(string value)
    {
        return PostgreSqlTableIdentifier.QuoteIdentifier(value);
    }
}
