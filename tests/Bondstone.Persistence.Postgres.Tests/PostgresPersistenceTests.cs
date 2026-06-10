using Bondstone.Messaging;
using Bondstone.Persistence.Postgres.Inbox;
using Bondstone.Persistence.Postgres.Operations;
using Bondstone.Persistence.Postgres.Outbox;
using Bondstone.Persistence.Postgres.Persistence;
using Dapper;
using Npgsql;
using Xunit;

namespace Bondstone.Persistence.Postgres.Tests;

public sealed class PostgresPersistenceTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task EnsureDurableMessagingTablesAsync_CreatesDurableTables()
    {
        const string schema = "dapper_schema";
        await ResetSchemaAsync(schema);

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(
            dataSource,
            schema);

        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync();
        string[] tables = (await connection.QueryAsync<string>(
            """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = @Schema
            AND table_name IN ('outbox_messages', 'inbox_messages', 'operation_states')
            ORDER BY table_name
            """,
            new { Schema = schema })).ToArray();

        Assert.Equal(
            ["inbox_messages", "operation_states", "outbox_messages"],
            tables);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteAsync_WhenTransactionCommits_PersistsOutboxMessage()
    {
        const string schema = "dapper_outbox_commit";
        await ResetSchemaAsync(schema);

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(
            dataSource,
            schema);
        await using var session = new PostgresModuleSession(dataSource);
        var writer = new PostgresDurableOutboxWriter(session, schema: schema);

        await session.ExecuteInTransactionAsync(
            async ct => await writer.WriteAsync(CreateEnvelope(), ct),
            CancellationToken.None);

        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync();
        int count = await connection.ExecuteScalarAsync<int>(
            """SELECT COUNT(*) FROM "dapper_outbox_commit"."outbox_messages" """);

        Assert.Equal(1, count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task WriteAsync_WhenTransactionRollsBack_DoesNotPersistOutboxMessage()
    {
        const string schema = "dapper_outbox_rollback";
        await ResetSchemaAsync(schema);

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(
            dataSource,
            schema);
        await using var session = new PostgresModuleSession(dataSource);
        var writer = new PostgresDurableOutboxWriter(session, schema: schema);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await session.ExecuteInTransactionAsync(
                async ct =>
                {
                    await writer.WriteAsync(CreateEnvelope(), ct);
                    throw new InvalidOperationException("rollback");
                },
                CancellationToken.None));

        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync();
        int count = await connection.ExecuteScalarAsync<int>(
            """SELECT COUNT(*) FROM "dapper_outbox_rollback"."outbox_messages" """);

        Assert.Equal(0, count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task HandleOnceAsync_WhenHandled_StoresProcessedInbox()
    {
        const string schema = "dapper_inbox";
        await ResetSchemaAsync(schema);

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(
            dataSource,
            schema);
        await using var session = new PostgresModuleSession(dataSource);
        var executor = new PostgresModuleDurableInboxHandlerExecutor(
            "billing",
            session,
            schema: schema);
        var record = new DurableInboxRecord(
            new DurableInboxMessageKey(Guid.NewGuid(), "billing", "billing.order.v1"),
            DateTimeOffset.UtcNow);

        await session.ExecuteInTransactionAsync(
            async ct => await executor.HandleOnceAsync(
                record,
                _ => ValueTask.CompletedTask,
                ct),
            CancellationToken.None);

        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync();
        int processedCount =
            await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM "dapper_inbox"."inbox_messages"
                WHERE "ProcessedAtUtc" IS NOT NULL
                """);

        Assert.Equal(1, processedCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SaveAsync_WhenStateExists_UpdatesOperationState()
    {
        const string schema = "dapper_operations";
        await ResetSchemaAsync(schema);

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(
            dataSource,
            schema);
        await using var session = new PostgresModuleSession(dataSource);
        var store = new PostgresDurableOperationStateStore(
            session,
            schema);
        Guid operationId = Guid.NewGuid();

        await session.ExecuteInTransactionAsync(
            async ct =>
            {
                await store.SaveAsync(new DurableOperationState(
                    operationId,
                    DurableOperationStatus.Pending,
                    DateTimeOffset.UtcNow), ct);
                await store.SaveAsync(new DurableOperationState(
                    operationId,
                    DurableOperationStatus.Completed,
                    DateTimeOffset.UtcNow), ct);
            },
            CancellationToken.None);

        DurableOperationState? state = await store.GetStateAsync(operationId);

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Completed, state.Status);
    }

    private async Task ResetSchemaAsync(string schema)
    {
        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            $"""DROP SCHEMA IF EXISTS "{schema}" CASCADE""");
    }

    private static DurableMessageEnvelope CreateEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.NewGuid(),
            MessageKind.Event,
            "ordering.order.placed.v1",
            "ordering",
            targetModule: null,
            payload: """{"orderId":"A-100"}""",
            createdAtUtc: DateTimeOffset.UtcNow);
    }
}
