using Bondstone.Persistence;
using Bondstone.Persistence.Dapper.Postgres.Persistence;
using Dapper;

namespace Bondstone.Persistence.Dapper.Postgres.Inbox;

public sealed class PostgresDapperDurableInboxRegistrar(
    IPostgresDapperModuleSession session,
    string? schema = null)
    : IDurableInboxRegistrar
{
    private readonly IPostgresDapperModuleSession _session =
        session ?? throw new ArgumentNullException(nameof(session));
    private readonly string _tableName = PostgresDapperTableIdentifier.Build(
        PostgresDapperDurableTableNames.InboxMessages,
        schema);

    public async ValueTask<DurableInboxRegistrationResult> RegisterAsync(
        DurableInboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await _session.EnsureOpenAsync(ct);
        DurableInboxMessageKey key = record.Key;
        PostgresDapperInboxRegistrationRow row =
            await _session.Connection.QuerySingleAsync<PostgresDapperInboxRegistrationRow>(
                new CommandDefinition(
                    $$"""
                    WITH inserted AS (
                        INSERT INTO {{_tableName}} (
                            "ModuleName", "MessageId", "HandlerIdentity",
                            "ReceivedAtUtc", "ProcessedAtUtc"
                        )
                        VALUES (
                            @ModuleName, @MessageId, @HandlerIdentity,
                            @ReceivedAtUtc, @ProcessedAtUtc
                        )
                        ON CONFLICT ON CONSTRAINT "PK_inbox_messages" DO NOTHING
                        RETURNING
                            "MessageId", "ModuleName", "HandlerIdentity",
                            "ReceivedAtUtc", "ProcessedAtUtc", TRUE AS "WasInserted"
                    )
                    SELECT
                        "MessageId", "ModuleName", "HandlerIdentity",
                        "ReceivedAtUtc", "ProcessedAtUtc", "WasInserted"
                    FROM inserted
                    UNION ALL
                    SELECT
                        "MessageId", "ModuleName", "HandlerIdentity",
                        "ReceivedAtUtc", "ProcessedAtUtc", FALSE AS "WasInserted"
                    FROM {{_tableName}}
                    WHERE "ModuleName" = @ModuleName
                    AND "MessageId" = @MessageId
                    AND "HandlerIdentity" = @HandlerIdentity
                    AND NOT EXISTS (SELECT 1 FROM inserted)
                    LIMIT 1
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

        DurableInboxRecord effectiveRecord = row.ToRecord();
        DurableInboxRegistrationStatus status = row.WasInserted
            ? DurableInboxRegistrationStatus.Registered
            : row.ProcessedAtUtc is null
                ? DurableInboxRegistrationStatus.AlreadyReceived
                : DurableInboxRegistrationStatus.AlreadyProcessed;

        return new DurableInboxRegistrationResult(status, effectiveRecord);
    }

    private sealed class PostgresDapperInboxRegistrationRow : PostgresDapperInboxRow
    {
        public bool WasInserted { get; init; }
    }
}
