using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bondstone.EntityFrameworkCore.Postgres.Outbox;

public sealed class PostgreSqlDurableOutboxLeaseRenewer<TDbContext>(
    TDbContext context,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableOutboxLeaseRenewer
    where TDbContext : DbContext
{
    private static readonly string MessageIdColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.MessageId);
    private static readonly string StatusColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.Status);
    private static readonly string ClaimedByColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.ClaimedBy);
    private static readonly string ClaimedUntilUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        OutboxMessageEntityConfiguration.Columns.ClaimedUntilUtc);

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly string _tableName = PostgreSqlOutboxTableIdentifier.BuildTableName(schema);

    public async ValueTask<bool> RenewAsync(
        Guid messageId,
        string claimedBy,
        TimeSpan leaseDuration,
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

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        DateTimeOffset renewedUntilUtc = nowUtc.Add(leaseDuration);

        string sql =
            $$"""
            UPDATE {{_tableName}}
            SET {{ClaimedUntilUtcColumn}} = @renewedUntilUtc
            WHERE {{MessageIdColumn}} = @messageId
            AND {{StatusColumn}} = @processing
            AND {{ClaimedByColumn}} = @claimedBy
            AND {{ClaimedUntilUtcColumn}} >= @nowUtc
            """;

        int rowCount = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("renewedUntilUtc", renewedUntilUtc),
                new NpgsqlParameter("messageId", messageId),
                new NpgsqlParameter("processing", DurableOutboxStatus.Processing.ToString()),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
                new NpgsqlParameter("nowUtc", nowUtc),
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
}
