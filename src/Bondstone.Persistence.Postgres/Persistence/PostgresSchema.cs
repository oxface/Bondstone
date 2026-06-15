using Dapper;
using Npgsql;

namespace Bondstone.Persistence.Postgres.Persistence;

public static class PostgresSchema
{
    public static async ValueTask EnsureDurableMessagingTablesAsync(
        NpgsqlDataSource dataSource,
        string? schema = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(ct);
        await EnsureDurableMessagingTablesAsync(connection, schema, ct);
    }

    public static async ValueTask EnsureDurableMessagingTablesAsync(
        NpgsqlConnection connection,
        string? schema = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (!string.IsNullOrWhiteSpace(schema))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                $"CREATE SCHEMA IF NOT EXISTS {PostgresTableIdentifier.QuoteIdentifier(schema.Trim())}",
                cancellationToken: ct));
        }

        string outboxTable = PostgresTableIdentifier.Build(
            PostgresDurableTableNames.OutboxMessages,
            schema);
        string inboxTable = PostgresTableIdentifier.Build(
            PostgresDurableTableNames.InboxMessages,
            schema);
        string operationTable = PostgresTableIdentifier.Build(
            PostgresDurableTableNames.OperationStates,
            schema);

        await connection.ExecuteAsync(new CommandDefinition(
            $$"""
            CREATE TABLE IF NOT EXISTS {{outboxTable}} (
                "MessageId" uuid NOT NULL,
                "MessageKind" character varying(32) NOT NULL,
                "MessageTypeName" character varying(256) NOT NULL,
                "SourceModule" character varying(128) NOT NULL,
                "TargetModule" character varying(128) NULL,
                "DurableOperationId" uuid NULL,
                "TraceParent" character varying(128) NULL,
                "TraceState" character varying(512) NULL,
                "TraceBaggage" text NULL,
                "CausationId" uuid NULL,
                "PartitionKey" character varying(512) NULL,
                "Payload" text NOT NULL,
                "Metadata" text NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "StoredAtUtc" timestamp with time zone NOT NULL,
                "Status" character varying(32) NOT NULL,
                "AttemptCount" integer NOT NULL,
                "NextAttemptAtUtc" timestamp with time zone NULL,
                "DispatchedAtUtc" timestamp with time zone NULL,
                "FailedAtUtc" timestamp with time zone NULL,
                "FailureReason" character varying(2048) NULL,
                "ClaimedBy" character varying(256) NULL,
                "ClaimedUntilUtc" timestamp with time zone NULL,
                CONSTRAINT "PK_outbox_messages" PRIMARY KEY ("MessageId")
            );
            CREATE INDEX IF NOT EXISTS "IX_outbox_messages_Status_NextAttemptAtUtc_StoredAtUtc"
                ON {{outboxTable}} ("Status", "NextAttemptAtUtc", "StoredAtUtc");
            CREATE INDEX IF NOT EXISTS "IX_outbox_messages_Status_ClaimedUntilUtc"
                ON {{outboxTable}} ("Status", "ClaimedUntilUtc");
            CREATE INDEX IF NOT EXISTS "IX_outbox_messages_MessageTypeName"
                ON {{outboxTable}} ("MessageTypeName");
            CREATE INDEX IF NOT EXISTS "IX_outbox_messages_DurableOperationId"
                ON {{outboxTable}} ("DurableOperationId");
            """,
            cancellationToken: ct));

        await connection.ExecuteAsync(new CommandDefinition(
            $$"""
            CREATE TABLE IF NOT EXISTS {{inboxTable}} (
                "MessageId" uuid NOT NULL,
                "ModuleName" character varying(128) NOT NULL,
                "HandlerIdentity" character varying(512) NOT NULL,
                "ReceivedAtUtc" timestamp with time zone NOT NULL,
                "ProcessedAtUtc" timestamp with time zone NULL,
                CONSTRAINT "PK_inbox_messages" PRIMARY KEY ("ModuleName", "MessageId", "HandlerIdentity")
            );
            CREATE INDEX IF NOT EXISTS "IX_inbox_messages_ReceivedAtUtc"
                ON {{inboxTable}} ("ReceivedAtUtc");
            """,
            cancellationToken: ct));

        await connection.ExecuteAsync(new CommandDefinition(
            $$"""
            CREATE TABLE IF NOT EXISTS {{operationTable}} (
                "DurableOperationId" uuid NOT NULL,
                "Status" character varying(32) NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NOT NULL,
                "ResultPayload" text NULL,
                "FailureReason" text NULL,
                "ModuleName" character varying(128) NULL,
                "MessageTypeName" character varying(256) NULL,
                "HandlerIdentity" character varying(512) NULL,
                CONSTRAINT "PK_operation_states" PRIMARY KEY ("DurableOperationId")
            );
            ALTER TABLE {{operationTable}}
                ADD COLUMN IF NOT EXISTS "ModuleName" character varying(128) NULL;
            ALTER TABLE {{operationTable}}
                ADD COLUMN IF NOT EXISTS "MessageTypeName" character varying(256) NULL;
            ALTER TABLE {{operationTable}}
                ADD COLUMN IF NOT EXISTS "HandlerIdentity" character varying(512) NULL;
            CREATE INDEX IF NOT EXISTS "IX_operation_states_Status"
                ON {{operationTable}} ("Status");
            CREATE INDEX IF NOT EXISTS "IX_operation_states_UpdatedAtUtc"
                ON {{operationTable}} ("UpdatedAtUtc");
            """,
            cancellationToken: ct));
    }
}
