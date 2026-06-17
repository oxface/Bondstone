using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.IncomingInbox;

internal sealed class PostgreSqlDurableIncomingInboxLeaseRenewer<TDbContext>(
    TDbContext context,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableIncomingInboxLeaseRenewer
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
    private static readonly string ClaimedByColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.ClaimedBy);
    private static readonly string ClaimedUntilUtcColumn = Quote(
        IncomingInboxMessageEntityConfiguration.Columns.ClaimedUntilUtc);

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly string _tableName = PostgreSqlIncomingInboxTableIdentifier.BuildTableName(schema);

    public async ValueTask<bool> RenewAsync(
        DurableIncomingInboxKey key,
        string claimedBy,
        TimeSpan leaseDuration,
        CancellationToken ct = default)
    {
        ValidateIncomingInboxMapping();
        ArgumentNullException.ThrowIfNull(key);
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
            WHERE {{ReceiverModuleColumn}} = @receiverModule
            AND {{MessageIdColumn}} = @messageId
            AND {{HandlerIdentityColumn}} = @handlerIdentity
            AND {{StatusColumn}} = @processing
            AND {{ClaimedByColumn}} = @claimedBy
            AND {{ClaimedUntilUtcColumn}} > @nowUtc
            """;

        int rowCount = await context.Database.ExecuteSqlRawAsync(
            sql,
            [
                new NpgsqlParameter("renewedUntilUtc", renewedUntilUtc),
                new NpgsqlParameter("receiverModule", key.ReceiverModule),
                new NpgsqlParameter("messageId", key.MessageId),
                new NpgsqlParameter("handlerIdentity", key.HandlerIdentity),
                new NpgsqlParameter("processing", DurableIncomingInboxStatus.Processing.ToString()),
                new NpgsqlParameter("claimedBy", normalizedClaimedBy),
                new NpgsqlParameter("nowUtc", nowUtc),
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

    private static string Quote(string value)
    {
        return PostgreSqlTableIdentifier.QuoteIdentifier(value);
    }
}
