using System.Data;
using System.Data.Common;
using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Bondstone.EntityFrameworkCore.Postgres.Inbox;

public sealed class PostgreSqlDurableInboxRegistrar<TDbContext>(
    TDbContext context,
    string? schema = null)
    : IDurableInboxRegistrar
    where TDbContext : DbContext
{
    private static readonly string MessageIdColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        InboxMessageEntityConfiguration.Columns.MessageId);
    private static readonly string ModuleNameColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        InboxMessageEntityConfiguration.Columns.ModuleName);
    private static readonly string HandlerIdentityColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        InboxMessageEntityConfiguration.Columns.HandlerIdentity);
    private static readonly string ReceivedAtUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        InboxMessageEntityConfiguration.Columns.ReceivedAtUtc);
    private static readonly string ProcessedAtUtcColumn = PostgreSqlTableIdentifier.QuoteIdentifier(
        InboxMessageEntityConfiguration.Columns.ProcessedAtUtc);

    private readonly string _tableName = PostgreSqlTableIdentifier.Build(
        InboxMessageEntityConfiguration.TableName,
        schema);

    public async ValueTask<DurableInboxRegistrationResult> RegisterAsync(
        DurableInboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableInboxMessageKey key = record.Key;
        string sql =
            $$"""
            WITH inserted AS (
                INSERT INTO {{_tableName}} (
                    {{ModuleNameColumn}},
                    {{MessageIdColumn}},
                    {{HandlerIdentityColumn}},
                    {{ReceivedAtUtcColumn}},
                    {{ProcessedAtUtcColumn}}
                )
                VALUES (
                    @moduleName,
                    @messageId,
                    @handlerIdentity,
                    @receivedAtUtc,
                    @processedAtUtc
                )
                ON CONFLICT ON CONSTRAINT "{{InboxMessageEntityConfiguration.PrimaryKeyName}}" DO NOTHING
                RETURNING
                    {{MessageIdColumn}},
                    {{ModuleNameColumn}},
                    {{HandlerIdentityColumn}},
                    {{ReceivedAtUtcColumn}},
                    {{ProcessedAtUtcColumn}},
                    TRUE AS "WasInserted"
            )
            SELECT
                {{MessageIdColumn}},
                {{ModuleNameColumn}},
                {{HandlerIdentityColumn}},
                {{ReceivedAtUtcColumn}},
                {{ProcessedAtUtcColumn}},
                "WasInserted"
            FROM inserted
            UNION ALL
            SELECT
                {{MessageIdColumn}},
                {{ModuleNameColumn}},
                {{HandlerIdentityColumn}},
                {{ReceivedAtUtcColumn}},
                {{ProcessedAtUtcColumn}},
                FALSE AS "WasInserted"
            FROM {{_tableName}}
            WHERE {{ModuleNameColumn}} = @moduleName
            AND {{MessageIdColumn}} = @messageId
            AND {{HandlerIdentityColumn}} = @handlerIdentity
            AND NOT EXISTS (SELECT 1 FROM inserted)
            LIMIT 1
            """;

        DbConnection connection = context.Database.GetDbConnection();
        ConnectionState originalState = connection.State;

        if (originalState != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();
            command.Parameters.Add(new NpgsqlParameter("moduleName", key.ModuleName));
            command.Parameters.Add(new NpgsqlParameter("messageId", key.MessageId));
            command.Parameters.Add(new NpgsqlParameter("handlerIdentity", key.HandlerIdentity));
            command.Parameters.Add(new NpgsqlParameter("receivedAtUtc", record.ReceivedAtUtc));
            command.Parameters.Add(new NpgsqlParameter(
                "processedAtUtc",
                record.ProcessedAtUtc is null ? DBNull.Value : record.ProcessedAtUtc));

            await using DbDataReader reader = await command.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
            {
                throw new InvalidOperationException("Inbox registration did not return a record.");
            }

            return ReadResult(reader);
        }
        finally
        {
            if (originalState != ConnectionState.Open)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static DurableInboxRegistrationResult ReadResult(DbDataReader reader)
    {
        Guid messageId = reader.GetGuid(0);
        string moduleName = reader.GetString(1);
        string handlerIdentity = reader.GetString(2);
        DateTimeOffset receivedAtUtc = reader.GetFieldValue<DateTimeOffset>(3);
        DateTimeOffset? processedAtUtc = reader.IsDBNull(4)
            ? null
            : reader.GetFieldValue<DateTimeOffset>(4);
        bool wasInserted = reader.GetBoolean(5);

        var record = new DurableInboxRecord(
            new DurableInboxMessageKey(messageId, moduleName, handlerIdentity),
            receivedAtUtc,
            processedAtUtc);
        DurableInboxRegistrationStatus status = wasInserted
            ? DurableInboxRegistrationStatus.Registered
            : processedAtUtc is null
                ? DurableInboxRegistrationStatus.AlreadyReceived
                : DurableInboxRegistrationStatus.AlreadyProcessed;

        return new DurableInboxRegistrationResult(status, record);
    }
}
