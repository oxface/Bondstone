using Bondstone.Persistence;
using Bondstone.Persistence.Dapper.Postgres.Persistence;
using Dapper;

namespace Bondstone.Persistence.Dapper.Postgres.Inbox;

public sealed class PostgresDapperDurableInboxStore(
    IPostgresDapperModuleSession session,
    string? schema = null)
    : IDurableInboxStore
{
    private readonly IPostgresDapperModuleSession _session =
        session ?? throw new ArgumentNullException(nameof(session));
    private readonly string _tableName = PostgresDapperTableIdentifier.Build(
        PostgresDapperDurableTableNames.InboxMessages,
        schema);

    public async ValueTask<DurableInboxRecord?> GetAsync(
        DurableInboxMessageKey key,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        await _session.EnsureOpenAsync(ct);
        PostgresDapperInboxRow? row =
            await _session.Connection.QuerySingleOrDefaultAsync<PostgresDapperInboxRow>(
                new CommandDefinition(
                    $$"""
                    SELECT "MessageId", "ModuleName", "HandlerIdentity",
                        "ReceivedAtUtc", "ProcessedAtUtc"
                    FROM {{_tableName}}
                    WHERE "ModuleName" = @ModuleName
                    AND "MessageId" = @MessageId
                    AND "HandlerIdentity" = @HandlerIdentity
                    """,
                    new
                    {
                        key.ModuleName,
                        key.MessageId,
                        key.HandlerIdentity,
                    },
                    _session.Transaction,
                    cancellationToken: ct));

        return row?.ToRecord();
    }

    public async ValueTask AddAsync(
        DurableInboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await _session.EnsureOpenAsync(ct);
        DurableInboxMessageKey key = record.Key;
        await _session.Connection.ExecuteAsync(new CommandDefinition(
            $$"""
            INSERT INTO {{_tableName}} (
                "ModuleName", "MessageId", "HandlerIdentity",
                "ReceivedAtUtc", "ProcessedAtUtc"
            )
            VALUES (
                @ModuleName, @MessageId, @HandlerIdentity,
                @ReceivedAtUtc, @ProcessedAtUtc
            )
            """,
            new
            {
                key.ModuleName,
                key.MessageId,
                key.HandlerIdentity,
                record.ReceivedAtUtc,
                record.ProcessedAtUtc,
            },
            _session.Transaction,
            cancellationToken: ct));
    }

    public async ValueTask MarkProcessedAsync(
        DurableInboxMessageKey key,
        DateTimeOffset processedAtUtc,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        await _session.EnsureOpenAsync(ct);
        int rowCount = await _session.Connection.ExecuteAsync(new CommandDefinition(
            $$"""
            UPDATE {{_tableName}}
            SET "ProcessedAtUtc" = @ProcessedAtUtc
            WHERE "ModuleName" = @ModuleName
            AND "MessageId" = @MessageId
            AND "HandlerIdentity" = @HandlerIdentity
            """,
            new
            {
                ProcessedAtUtc = processedAtUtc,
                key.ModuleName,
                key.MessageId,
                key.HandlerIdentity,
            },
            _session.Transaction,
            cancellationToken: ct));

        if (rowCount != 1)
        {
            throw new InvalidOperationException("Inbox message was not found.");
        }
    }
}
