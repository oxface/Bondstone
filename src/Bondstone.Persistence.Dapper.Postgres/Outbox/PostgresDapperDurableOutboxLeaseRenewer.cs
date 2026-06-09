using Bondstone.Persistence;
using Bondstone.Persistence.Dapper.Postgres.Persistence;
using Dapper;
using Npgsql;

namespace Bondstone.Persistence.Dapper.Postgres.Outbox;

public sealed class PostgresDapperDurableOutboxLeaseRenewer(
    NpgsqlDataSource dataSource,
    TimeProvider? timeProvider = null,
    string? schema = null)
    : IDurableOutboxLeaseRenewer
{
    private readonly NpgsqlDataSource _dataSource =
        dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly string _tableName = PostgresDapperTableIdentifier.Build(
        PostgresDapperDurableTableNames.OutboxMessages,
        schema);

    public async ValueTask<bool> RenewAsync(
        Guid messageId,
        string claimedBy,
        TimeSpan leaseDuration,
        CancellationToken ct = default)
    {
        string normalizedClaimedBy = NormalizeClaimedBy(claimedBy);
        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration));
        }

        DateTimeOffset nowUtc = _timeProvider.GetUtcNow();
        DateTimeOffset claimedUntilUtc = nowUtc.Add(leaseDuration);
        await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(ct);
        int rowCount = await connection.ExecuteAsync(new CommandDefinition(
            $$"""
            UPDATE {{_tableName}}
            SET "ClaimedUntilUtc" = @ClaimedUntilUtc
            WHERE "MessageId" = @MessageId
            AND "Status" = @Processing
            AND "ClaimedBy" = @ClaimedBy
            AND "ClaimedUntilUtc" >= @NowUtc
            """,
            new
            {
                ClaimedUntilUtc = claimedUntilUtc,
                MessageId = messageId,
                Processing = DurableOutboxStatus.Processing.ToString(),
                ClaimedBy = normalizedClaimedBy,
                NowUtc = nowUtc,
            },
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
}
