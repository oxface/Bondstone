using Bondstone.Messaging;
using Bondstone.Persistence;
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
    public async Task OutboxClaimer_WhenProcessingLeaseExpired_ReclaimsMessage()
    {
        const string schema = "dapper_outbox_reclaim_expired";
        await ResetSchemaAsync(schema);
        Guid messageId = Guid.Parse("5815e856-43b3-4efa-9e05-6625826fd3e0");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-04T00:05:00+00:00");

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(dataSource, schema);
        await WriteOutboxMessageAsync(dataSource, schema, messageId);
        await MarkOutboxMessageProcessingAsync(
            dataSource,
            schema,
            messageId,
            "expired-dispatcher",
            DateTimeOffset.Parse("2026-06-04T00:04:59+00:00"));

        var claimer = new PostgresDurableOutboxClaimer(
            dataSource,
            new FixedTimeProvider(claimTimeUtc),
            schema);

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        DurableOutboxRecord record = Assert.Single(records);
        Assert.Equal(messageId, record.Envelope.MessageId);
        Assert.Equal(DurableOutboxStatus.Processing, record.DispatchState.Status);
        Assert.Equal(2, record.DispatchState.AttemptCount);
        Assert.Equal("dispatcher-1", record.DispatchState.ClaimedBy);
        Assert.Equal(claimTimeUtc.AddMinutes(5), record.DispatchState.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenProcessingLeaseIsActive_DoesNotReclaimMessage()
    {
        const string schema = "dapper_outbox_reclaim_active";
        await ResetSchemaAsync(schema);
        Guid messageId = Guid.Parse("f130352f-4d3a-4dcf-9194-e7c0d5d7aac6");

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(dataSource, schema);
        await WriteOutboxMessageAsync(dataSource, schema, messageId);
        await MarkOutboxMessageProcessingAsync(
            dataSource,
            schema,
            messageId,
            "active-dispatcher",
            DateTimeOffset.Parse("2026-06-04T00:05:01+00:00"));

        var claimer = new PostgresDurableOutboxClaimer(
            dataSource,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:05:00+00:00")),
            schema);

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Empty(records);

        OutboxState state = await ReadOutboxStateAsync(dataSource, schema, messageId);
        Assert.Equal(DurableOutboxStatus.Processing.ToString(), state.Status);
        Assert.Equal(1, state.AttemptCount);
        Assert.Equal("active-dispatcher", state.ClaimedBy);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatchRecorder_WhenTerminalFailureIsRecorded_ClearsClaimState()
    {
        const string schema = "dapper_outbox_terminal_failure";
        await ResetSchemaAsync(schema);
        Guid messageId = Guid.Parse("23b191d3-3e24-4746-83a0-3402eb19f309");
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(dataSource, schema);
        await WriteOutboxMessageAsync(dataSource, schema, messageId);
        await MarkOutboxMessageProcessingAsync(
            dataSource,
            schema,
            messageId,
            "dispatcher-1",
            DateTimeOffset.Parse("2026-06-04T00:05:00+00:00"));
        var recorder = new PostgresDurableOutboxDispatchRecorder(dataSource, schema);

        bool updated = await recorder.MarkTerminalFailedAsync(
            messageId,
            "dispatcher-1",
            "poison message",
            failedAtUtc);

        Assert.True(updated);

        OutboxState state = await ReadOutboxStateAsync(dataSource, schema, messageId);
        Assert.Equal(DurableOutboxStatus.TerminalFailed.ToString(), state.Status);
        Assert.Equal(failedAtUtc, state.FailedAtUtc);
        Assert.Equal("poison message", state.FailureReason);
        Assert.Null(state.NextAttemptAtUtc);
        Assert.Null(state.DispatchedAtUtc);
        Assert.Null(state.ClaimedBy);
        Assert.Null(state.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatchRecorder_WhenClaimIsOwnedByAnotherWorker_ReturnsFalse()
    {
        const string schema = "dapper_outbox_wrong_claimant";
        await ResetSchemaAsync(schema);
        Guid messageId = Guid.Parse("73cb3669-0ed1-4cfd-8257-a799d0ed2591");
        DateTimeOffset originalClaimedUntilUtc = DateTimeOffset.Parse("2026-06-04T00:05:00+00:00");

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(dataSource, schema);
        await WriteOutboxMessageAsync(dataSource, schema, messageId);
        await MarkOutboxMessageProcessingAsync(
            dataSource,
            schema,
            messageId,
            "dispatcher-1",
            originalClaimedUntilUtc);
        var recorder = new PostgresDurableOutboxDispatchRecorder(dataSource, schema);

        bool updated = await recorder.MarkDispatchedAsync(
            messageId,
            "dispatcher-2",
            DateTimeOffset.Parse("2026-06-04T00:02:00+00:00"));

        Assert.False(updated);

        OutboxState state = await ReadOutboxStateAsync(dataSource, schema, messageId);
        Assert.Equal(DurableOutboxStatus.Processing.ToString(), state.Status);
        Assert.Null(state.DispatchedAtUtc);
        Assert.Equal("dispatcher-1", state.ClaimedBy);
        Assert.Equal(originalClaimedUntilUtc, state.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatchRecorder_WhenClaimLeaseExpired_ReturnsFalse()
    {
        const string schema = "dapper_outbox_expired_claim_record";
        await ResetSchemaAsync(schema);
        Guid messageId = Guid.Parse("edb275d6-f5e7-4282-8f19-852d08b141e6");
        DateTimeOffset originalClaimedUntilUtc = DateTimeOffset.Parse("2026-06-04T00:01:59+00:00");

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(dataSource, schema);
        await WriteOutboxMessageAsync(dataSource, schema, messageId);
        await MarkOutboxMessageProcessingAsync(
            dataSource,
            schema,
            messageId,
            "dispatcher-1",
            originalClaimedUntilUtc);
        var recorder = new PostgresDurableOutboxDispatchRecorder(dataSource, schema);

        bool updated = await recorder.MarkDispatchedAsync(
            messageId,
            "dispatcher-1",
            DateTimeOffset.Parse("2026-06-04T00:02:00+00:00"));

        Assert.False(updated);

        OutboxState state = await ReadOutboxStateAsync(dataSource, schema, messageId);
        Assert.Equal(DurableOutboxStatus.Processing.ToString(), state.Status);
        Assert.Null(state.DispatchedAtUtc);
        Assert.Equal("dispatcher-1", state.ClaimedBy);
        Assert.Equal(originalClaimedUntilUtc, state.ClaimedUntilUtc);
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
    public async Task InboxRegistrar_WhenRecordDoesNotExist_RegistersInboxRecord()
    {
        const string schema = "dapper_inbox_register";
        await ResetSchemaAsync(schema);
        DurableInboxRecord record = CreateInboxRecord();

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(dataSource, schema);
        await using var session = new PostgresModuleSession(dataSource);
        var registrar = new PostgresDurableInboxRegistrar(session, schema);

        DurableInboxRegistrationResult result = await registrar.RegisterAsync(record);

        Assert.Equal(DurableInboxRegistrationStatus.Registered, result.Status);
        Assert.True(result.IsRegistered);
        Assert.False(result.IsDuplicate);
        Assert.Equal(record, result.Record);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InboxRegistrar_WhenDuplicateRecordIsUnprocessed_ReturnsAlreadyReceived()
    {
        const string schema = "dapper_inbox_duplicate_unprocessed";
        await ResetSchemaAsync(schema);
        DurableInboxRecord record = CreateInboxRecord();

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(dataSource, schema);
        await using (var setupSession = new PostgresModuleSession(dataSource))
        {
            var setupRegistrar = new PostgresDurableInboxRegistrar(setupSession, schema);
            await setupRegistrar.RegisterAsync(record);
        }

        await using var duplicateSession = new PostgresModuleSession(dataSource);
        var duplicateRegistrar = new PostgresDurableInboxRegistrar(duplicateSession, schema);

        DurableInboxRegistrationResult result = await duplicateRegistrar.RegisterAsync(
            new DurableInboxRecord(
                record.Key,
                DateTimeOffset.Parse("2026-06-04T00:05:00+00:00")));

        Assert.Equal(DurableInboxRegistrationStatus.AlreadyReceived, result.Status);
        Assert.False(result.IsRegistered);
        Assert.True(result.IsDuplicate);
        Assert.Equal(record, result.Record);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InboxRegistrar_WhenDuplicateRecordIsProcessed_ReturnsAlreadyProcessed()
    {
        const string schema = "dapper_inbox_duplicate_processed";
        await ResetSchemaAsync(schema);
        DurableInboxRecord record = CreateInboxRecord();
        DateTimeOffset processedAtUtc = DateTimeOffset.Parse("2026-06-04T00:03:00+00:00");

        await using NpgsqlDataSource dataSource =
            NpgsqlDataSource.Create(fixture.ConnectionString);
        await PostgresSchema.EnsureDurableMessagingTablesAsync(dataSource, schema);
        await using (var setupSession = new PostgresModuleSession(dataSource))
        {
            var setupRegistrar = new PostgresDurableInboxRegistrar(setupSession, schema);
            var store = new PostgresDurableInboxStore(setupSession, schema);
            await setupRegistrar.RegisterAsync(record);
            await store.MarkProcessedAsync(record.Key, processedAtUtc);
        }

        await using var duplicateSession = new PostgresModuleSession(dataSource);
        var duplicateRegistrar = new PostgresDurableInboxRegistrar(duplicateSession, schema);

        DurableInboxRegistrationResult result = await duplicateRegistrar.RegisterAsync(record);

        Assert.Equal(DurableInboxRegistrationStatus.AlreadyProcessed, result.Status);
        Assert.False(result.IsRegistered);
        Assert.True(result.IsDuplicate);
        Assert.Equal(processedAtUtc, result.Record.ProcessedAtUtc);
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

    private static async Task WriteOutboxMessageAsync(
        NpgsqlDataSource dataSource,
        string schema,
        Guid messageId)
    {
        await using var session = new PostgresModuleSession(dataSource);
        var writer = new PostgresDurableOutboxWriter(
            session,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")),
            schema);

        await writer.WriteAsync(CreateEnvelope(messageId));
    }

    private static async Task MarkOutboxMessageProcessingAsync(
        NpgsqlDataSource dataSource,
        string schema,
        Guid messageId,
        string claimedBy,
        DateTimeOffset claimedUntilUtc)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            $"""
            UPDATE "{schema}"."outbox_messages"
            SET "Status" = @Status,
                "AttemptCount" = 1,
                "ClaimedBy" = @ClaimedBy,
                "ClaimedUntilUtc" = @ClaimedUntilUtc
            WHERE "MessageId" = @MessageId
            """,
            new
            {
                Status = DurableOutboxStatus.Processing.ToString(),
                ClaimedBy = claimedBy,
                ClaimedUntilUtc = claimedUntilUtc,
                MessageId = messageId,
            });
    }

    private static async Task<OutboxState> ReadOutboxStateAsync(
        NpgsqlDataSource dataSource,
        string schema,
        Guid messageId)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleAsync<OutboxState>(
            $"""
            SELECT "Status", "AttemptCount", "NextAttemptAtUtc", "DispatchedAtUtc",
                "FailedAtUtc", "FailureReason", "ClaimedBy", "ClaimedUntilUtc"
            FROM "{schema}"."outbox_messages"
            WHERE "MessageId" = @MessageId
            """,
            new { MessageId = messageId });
    }

    private static DurableInboxRecord CreateInboxRecord()
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(
                Guid.Parse("95ff6ad6-2e0a-47af-a480-7ef308b5223b"),
                "billing",
                "billing.order.v1"),
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
    }

    private static DurableMessageEnvelope CreateEnvelope()
    {
        return CreateEnvelope(Guid.NewGuid());
    }

    private static DurableMessageEnvelope CreateEnvelope(Guid messageId)
    {
        return new DurableMessageEnvelope(
            messageId,
            MessageKind.Event,
            "ordering.order.placed.v1",
            "ordering",
            targetModule: null,
            payload: """{"orderId":"A-100"}""",
            createdAtUtc: DateTimeOffset.UtcNow);
    }

    private sealed class OutboxState
    {
        public string Status { get; init; } = string.Empty;

        public int AttemptCount { get; init; }

        public DateTimeOffset? NextAttemptAtUtc { get; init; }

        public DateTimeOffset? DispatchedAtUtc { get; init; }

        public DateTimeOffset? FailedAtUtc { get; init; }

        public string? FailureReason { get; init; }

        public string? ClaimedBy { get; init; }

        public DateTimeOffset? ClaimedUntilUtc { get; init; }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
