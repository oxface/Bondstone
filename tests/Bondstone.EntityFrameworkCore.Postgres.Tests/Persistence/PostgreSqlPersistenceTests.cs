using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed class PostgreSqlPersistenceTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplyBondstonePersistence_WhenDatabaseIsCreated_CreatesBondstoneTables()
    {
        await ResetDatabaseAsync();
        await using PostgreSqlTestDbContext context = CreateContext();

        string[] tableNames = await context
            .Database
            .SqlQueryRaw<string>(
                """
                SELECT table_name AS "Value"
                FROM information_schema.tables
                WHERE table_schema = 'public'
                AND table_name IN ('outbox_messages', 'inbox_messages', 'operation_states')
                ORDER BY table_name
                """)
            .ToArrayAsync();

        Assert.Equal(
            ["inbox_messages", "operation_states", "outbox_messages"],
            tableNames);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplyBondstonePersistence_WhenDatabaseIsCreated_CreatesExpectedPrimaryKeys()
    {
        await ResetDatabaseAsync();
        await using PostgreSqlTestDbContext context = CreateContext();

        string[] primaryKeys = await context
            .Database
            .SqlQueryRaw<string>(
                """
                SELECT rel.relname || ':' || con.conname AS "Value"
                FROM pg_constraint con
                JOIN pg_class rel ON rel.oid = con.conrelid
                JOIN pg_namespace nsp ON nsp.oid = rel.relnamespace
                WHERE con.contype = 'p'
                AND nsp.nspname = 'public'
                AND rel.relname IN ('outbox_messages', 'inbox_messages', 'operation_states')
                ORDER BY rel.relname
                """)
            .ToArrayAsync();

        Assert.Equal(
            [
                $"inbox_messages:{InboxMessageEntityConfiguration.PrimaryKeyName}",
                "operation_states:PK_operation_states",
                "outbox_messages:PK_outbox_messages",
            ],
            primaryKeys);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ApplyBondstonePersistence_WhenDatabaseIsCreated_CreatesOutboxClaimLeaseColumns()
    {
        await ResetDatabaseAsync();
        await using PostgreSqlTestDbContext context = CreateContext();

        string[] columnNames = await context
            .Database
            .SqlQueryRaw<string>(
                """
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_schema = 'public'
                AND table_name = 'outbox_messages'
                AND column_name IN ('ClaimedBy', 'ClaimedUntilUtc')
                ORDER BY column_name
                """)
            .ToArrayAsync();

        Assert.Equal(["ClaimedBy", "ClaimedUntilUtc"], columnNames);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxWriter_WhenTransactionRollsBack_DoesNotPersistMessage()
    {
        await ResetDatabaseAsync();
        DurableMessageEnvelope envelope = CreateEnvelope();

        await using (PostgreSqlTestDbContext context = CreateContext())
        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync())
        {
            var writer = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                context,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

            await writer.WriteAsync(envelope);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        Assert.Equal(0, await verificationContext.Set<OutboxMessageEntity>().CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxWriter_WhenTransactionCommits_PersistsMessage()
    {
        await ResetDatabaseAsync();
        DurableMessageEnvelope envelope = CreateEnvelope();

        await using (PostgreSqlTestDbContext context = CreateContext())
        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync())
        {
            var writer = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                context,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

            await writer.WriteAsync(envelope);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync();

        Assert.Equal(envelope.MessageId, entity.MessageId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InboxStore_WhenDuplicateRecordIsSaved_ThrowsPostgreSqlUniqueViolation()
    {
        await ResetDatabaseAsync();
        DurableInboxRecord record = CreateInboxRecord();

        await using (PostgreSqlTestDbContext context = CreateContext())
        {
            var store = new EntityFrameworkCoreDurableInboxStore<PostgreSqlTestDbContext>(context);

            await store.AddAsync(record);
            await context.SaveChangesAsync();
        }

        await using PostgreSqlTestDbContext duplicateContext = CreateContext();
        var duplicateStore = new EntityFrameworkCoreDurableInboxStore<PostgreSqlTestDbContext>(duplicateContext);
        await duplicateStore.AddAsync(record);

        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => duplicateContext.SaveChangesAsync());

        Assert.True(PostgreSqlPersistenceExceptionClassifier.IsUniqueViolation(exception));
        Assert.True(PostgreSqlPersistenceExceptionClassifier.IsInboxMessageDuplicate(exception));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InboxStore_WhenDuplicateFailsInsideSavepoint_CanRollbackAndCommitOtherWork()
    {
        await ResetDatabaseAsync();
        DurableInboxRecord record = CreateInboxRecord();
        DurableMessageEnvelope envelope = CreateEnvelope(Guid.Parse("d37cd154-7c7d-4974-8440-6ed171e44175"));

        await using (PostgreSqlTestDbContext context = CreateContext())
        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync())
        {
            var inboxStore = new EntityFrameworkCoreDurableInboxStore<PostgreSqlTestDbContext>(context);
            await inboxStore.AddAsync(record);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            await transaction.CreateSavepointAsync("before_duplicate");

            await inboxStore.AddAsync(record);
            DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
                () => context.SaveChangesAsync());

            Assert.True(PostgreSqlPersistenceExceptionClassifier.IsInboxMessageDuplicate(exception));

            await transaction.RollbackToSavepointAsync("before_duplicate");
            context.ChangeTracker.Clear();

            var outboxWriter = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                context,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:05+00:00")));
            await outboxWriter.WriteAsync(envelope);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        Assert.Equal(1, await verificationContext.Set<InboxMessageEntity>().CountAsync());
        Assert.Equal(envelope.MessageId, await verificationContext
            .Set<OutboxMessageEntity>()
            .Select(static entity => entity.MessageId)
            .SingleAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxQuery_WhenFirstPendingMessageIsLocked_SkipLockedReturnsNextMessage()
    {
        await ResetDatabaseAsync();
        Guid firstMessageId = Guid.Parse("151aa1ba-f54f-49be-88f6-297584db1a4f");
        Guid secondMessageId = Guid.Parse("afec9cab-6a1f-4e30-ab31-4f88f71c94f9");

        await using (PostgreSqlTestDbContext setupContext = CreateContext())
        {
            var firstWriter = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                setupContext,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));
            var secondWriter = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                setupContext,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));

            await firstWriter.WriteAsync(CreateEnvelope(firstMessageId));
            await secondWriter.WriteAsync(CreateEnvelope(secondMessageId));
            await setupContext.SaveChangesAsync();
        }

        await using PostgreSqlTestDbContext firstContext = CreateContext();
        await using PostgreSqlTestDbContext secondContext = CreateContext();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction firstTransaction =
            await firstContext.Database.BeginTransactionAsync();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction secondTransaction =
            await secondContext.Database.BeginTransactionAsync();

        Guid lockedMessageId = await SelectNextPendingOutboxMessageForUpdateAsync(firstContext);
        Guid skippedLockedMessageId = await SelectNextPendingOutboxMessageForUpdateAsync(secondContext);

        await firstTransaction.RollbackAsync();
        await secondTransaction.RollbackAsync();

        Assert.Equal(firstMessageId, lockedMessageId);
        Assert.Equal(secondMessageId, skippedLockedMessageId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenPendingMessagesExist_ClaimsDueMessagesAndWritesLeaseState()
    {
        await ResetDatabaseAsync();
        Guid firstMessageId = Guid.Parse("3c1c12e5-7d86-4e28-964d-17b3d0245f9a");
        Guid secondMessageId = Guid.Parse("4d48c716-5574-4f55-ae18-c3a22cc577ef");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-04T00:01:00+00:00");

        await WriteOutboxMessagesAsync(
            (firstMessageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")),
            (secondMessageId, DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));

        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(claimTimeUtc));

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            " dispatcher-1 ",
            TimeSpan.FromMinutes(5),
            maxCount: 1);

        Assert.Single(records);
        DurableOutboxRecord record = records[0];
        Assert.Equal(firstMessageId, record.Envelope.MessageId);
        Assert.Equal(DurableOutboxStatus.Processing, record.DispatchState.Status);
        Assert.Equal(1, record.DispatchState.AttemptCount);
        Assert.Equal("dispatcher-1", record.DispatchState.ClaimedBy);
        Assert.Equal(claimTimeUtc.AddMinutes(5), record.DispatchState.ClaimedUntilUtc);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity firstEntity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == firstMessageId);
        OutboxMessageEntity secondEntity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == secondMessageId);

        Assert.Equal(DurableOutboxStatus.Processing, firstEntity.Status);
        Assert.Equal(1, firstEntity.AttemptCount);
        Assert.Equal("dispatcher-1", firstEntity.ClaimedBy);
        Assert.Equal(claimTimeUtc.AddMinutes(5), firstEntity.ClaimedUntilUtc);
        Assert.Equal(DurableOutboxStatus.Pending, secondEntity.Status);
        Assert.Null(secondEntity.ClaimedBy);
        Assert.Null(secondEntity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenPendingMessagesAreScheduled_ClaimsOnlyDueMessages()
    {
        await ResetDatabaseAsync();
        Guid dueMessageId = Guid.Parse("c5641848-40d1-4061-9084-60c62a270d92");
        Guid futureMessageId = Guid.Parse("d760376e-f1c1-4be0-8bc4-5e5b7f5cd1f8");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-04T00:05:00+00:00");

        await WriteOutboxMessagesAsync(
            (dueMessageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")),
            (futureMessageId, DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));
        await MarkOutboxMessageNextAttemptAsync(
            dueMessageId,
            DateTimeOffset.Parse("2026-06-04T00:04:59+00:00"));
        await MarkOutboxMessageNextAttemptAsync(
            futureMessageId,
            DateTimeOffset.Parse("2026-06-04T00:05:01+00:00"));

        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(claimTimeUtc));

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5),
            maxCount: 10);

        DurableOutboxRecord record = Assert.Single(records);
        Assert.Equal(dueMessageId, record.Envelope.MessageId);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity futureEntity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == futureMessageId);

        Assert.Equal(DurableOutboxStatus.Pending, futureEntity.Status);
        Assert.Null(futureEntity.ClaimedBy);
        Assert.Null(futureEntity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenFirstClaimTransactionIsOpen_SecondClaimerSkipsLockedMessage()
    {
        await ResetDatabaseAsync();
        Guid firstMessageId = Guid.Parse("ad1779e2-70dd-4441-8f9a-25d8a5208f97");
        Guid secondMessageId = Guid.Parse("d8cb565b-b68e-40e5-ac22-4fb2c4da2d5f");

        await WriteOutboxMessagesAsync(
            (firstMessageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")),
            (secondMessageId, DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));

        await using PostgreSqlTestDbContext firstContext = CreateContext();
        await using PostgreSqlTestDbContext secondContext = CreateContext();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction firstTransaction =
            await firstContext.Database.BeginTransactionAsync();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction secondTransaction =
            await secondContext.Database.BeginTransactionAsync();

        var firstClaimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            firstContext,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:01:00+00:00")));
        var secondClaimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            secondContext,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:01:01+00:00")));

        IReadOnlyList<DurableOutboxRecord> firstClaim = await firstClaimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5),
            maxCount: 1);
        IReadOnlyList<DurableOutboxRecord> secondClaim = await secondClaimer.ClaimAsync(
            "dispatcher-2",
            TimeSpan.FromMinutes(5),
            maxCount: 1);

        await firstTransaction.RollbackAsync();
        await secondTransaction.RollbackAsync();

        DurableOutboxRecord firstRecord = Assert.Single(firstClaim);
        DurableOutboxRecord secondRecord = Assert.Single(secondClaim);
        Assert.Equal(firstMessageId, firstRecord.Envelope.MessageId);
        Assert.Equal(secondMessageId, secondRecord.Envelope.MessageId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenProcessingLeaseExpired_ReclaimsMessage()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("17c57a8e-af36-47e1-9cfd-dbcc0b1d3998");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-04T00:05:00+00:00");

        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));
        await MarkOutboxMessageProcessingAsync(
            messageId,
            "expired-dispatcher",
            DateTimeOffset.Parse("2026-06-04T00:04:59+00:00"));

        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(claimTimeUtc));

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
    public async Task OutboxClaimer_WhenProcessingLeaseIsActive_DoesNotClaimMessage()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("397999d9-7e4d-4979-90f1-9d00f03e23a0");

        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));
        await MarkOutboxMessageProcessingAsync(
            messageId,
            "active-dispatcher",
            DateTimeOffset.Parse("2026-06-04T00:05:01+00:00"));

        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:05:00+00:00")));

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Empty(records);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InboxStore_WhenMessageIsMarkedProcessed_PersistsProcessedTimestamp()
    {
        await ResetDatabaseAsync();
        DurableInboxRecord record = CreateInboxRecord();
        DateTimeOffset processedAtUtc = DateTimeOffset.Parse("2026-06-04T00:00:03+00:00");

        await using (PostgreSqlTestDbContext context = CreateContext())
        {
            var store = new EntityFrameworkCoreDurableInboxStore<PostgreSqlTestDbContext>(context);

            await store.AddAsync(record);
            await context.SaveChangesAsync();
            await store.MarkProcessedAsync(record.Key, processedAtUtc);
            await context.SaveChangesAsync();
        }

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        InboxMessageEntity entity = await verificationContext
            .Set<InboxMessageEntity>()
            .SingleAsync();

        Assert.Equal(processedAtUtc, entity.ProcessedAtUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OperationStateStore_WhenStateIsSavedAgain_UpdatesExistingState()
    {
        await ResetDatabaseAsync();
        Guid durableOperationId = Guid.Parse("0d033b57-1153-498f-aac8-9ed1a3ac9562");

        await using (PostgreSqlTestDbContext context = CreateContext())
        {
            var store = new EntityFrameworkCoreDurableOperationStateStore<PostgreSqlTestDbContext>(context);
            var pendingState = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-04T00:00:01+00:00"));
            var completedState = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-04T00:00:04+00:00"),
                resultPayload: """{"status":"ok"}""");

            await store.SaveAsync(pendingState);
            await context.SaveChangesAsync();
            await store.SaveAsync(completedState);
            await context.SaveChangesAsync();
        }

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OperationStateEntity entity = await verificationContext
            .Set<OperationStateEntity>()
            .SingleAsync();

        Assert.Equal(DurableOperationStatus.Completed, entity.Status);
        Assert.Equal("""{"status":"ok"}""", entity.ResultPayload);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddBondstonePostgreSqlPersistence_WhenResolved_UsesPostgreSqlStores()
    {
        await ResetDatabaseAsync();
        var services = new ServiceCollection();
        services.AddBondstonePostgreSqlPersistence<PostgreSqlTestDbContext>(fixture.ConnectionString);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IDurableOutboxWriter writer = scope.ServiceProvider.GetRequiredService<IDurableOutboxWriter>();
        PostgreSqlTestDbContext context = scope.ServiceProvider.GetRequiredService<PostgreSqlTestDbContext>();
        DurableMessageEnvelope envelope = CreateEnvelope();

        await writer.WriteAsync(envelope);
        await context.SaveChangesAsync();

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync();

        Assert.Equal(envelope.MessageId, entity.MessageId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddBondstonePostgreSqlPersistence_WhenSchemaConfigured_UsesSchemaForRegisteredClaimer()
    {
        await ResetSchemaDatabaseAsync();
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-04T00:01:00+00:00");
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(claimTimeUtc));
        services.AddBondstonePostgreSqlPersistence<PostgreSqlSchemaTestDbContext>(
            fixture.ConnectionString,
            schema: PostgreSqlSchemaTestDbContext.BondstoneSchema);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IDurableOutboxWriter writer = scope.ServiceProvider.GetRequiredService<IDurableOutboxWriter>();
        IDurableOutboxClaimer claimer = scope.ServiceProvider.GetRequiredService<IDurableOutboxClaimer>();
        PostgreSqlSchemaTestDbContext context =
            scope.ServiceProvider.GetRequiredService<PostgreSqlSchemaTestDbContext>();
        DurableMessageEnvelope envelope = CreateEnvelope(Guid.Parse("55f93286-f946-481c-9651-2834dd2a253d"));

        await writer.WriteAsync(envelope);
        await context.SaveChangesAsync();

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        DurableOutboxRecord record = Assert.Single(records);
        Assert.Equal(envelope.MessageId, record.Envelope.MessageId);
        Assert.Equal(DurableOutboxStatus.Processing, record.DispatchState.Status);
        Assert.Equal("dispatcher-1", record.DispatchState.ClaimedBy);
        Assert.Equal(claimTimeUtc.AddMinutes(5), record.DispatchState.ClaimedUntilUtc);
    }

    private async Task ResetDatabaseAsync()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private PostgreSqlTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlTestDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        return new PostgreSqlTestDbContext(options);
    }

    private async Task ResetSchemaDatabaseAsync()
    {
        await using PostgreSqlSchemaTestDbContext context = CreateSchemaContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private PostgreSqlSchemaTestDbContext CreateSchemaContext()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlSchemaTestDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        return new PostgreSqlSchemaTestDbContext(options);
    }

    private static async Task<Guid> SelectNextPendingOutboxMessageForUpdateAsync(
        PostgreSqlTestDbContext context)
    {
        return await context
            .Database
            .SqlQueryRaw<Guid>(
                """
                SELECT "MessageId" AS "Value"
                FROM outbox_messages
                WHERE "Status" = 'Pending'
                ORDER BY "StoredAtUtc", "MessageId"
                FOR UPDATE SKIP LOCKED
                LIMIT 1
                """)
            .SingleAsync();
    }

    private async Task WriteOutboxMessagesAsync(
        params (Guid MessageId, DateTimeOffset StoredAtUtc)[] messages)
    {
        await using PostgreSqlTestDbContext context = CreateContext();

        foreach ((Guid messageId, DateTimeOffset storedAtUtc) in messages)
        {
            var writer = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                context,
                new FixedTimeProvider(storedAtUtc));

            await writer.WriteAsync(CreateEnvelope(messageId));
        }

        await context.SaveChangesAsync();
    }

    private async Task MarkOutboxMessageProcessingAsync(
        Guid messageId,
        string claimedBy,
        DateTimeOffset claimedUntilUtc)
    {
        await using PostgreSqlTestDbContext context = CreateContext();

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE outbox_messages
            SET
                "Status" = 'Processing',
                "AttemptCount" = 1,
                "ClaimedBy" = {claimedBy},
                "ClaimedUntilUtc" = {claimedUntilUtc}
            WHERE "MessageId" = {messageId}
            """);
    }

    private async Task MarkOutboxMessageNextAttemptAsync(
        Guid messageId,
        DateTimeOffset nextAttemptAtUtc)
    {
        await using PostgreSqlTestDbContext context = CreateContext();

        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE outbox_messages
            SET "NextAttemptAtUtc" = {nextAttemptAtUtc}
            WHERE "MessageId" = {messageId}
            """);
    }

    private static DurableMessageEnvelope CreateEnvelope(Guid? messageId = null)
    {
        return new DurableMessageEnvelope(
            messageId ?? Guid.Parse("48cb19e0-3689-4ec7-b629-8f8e19916d43"),
            MessageKind.Command,
            "orders.submit.v1",
            "sales",
            "fulfillment",
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"),
            durableOperationId: Guid.Parse("a0e7c46f-2699-40ec-888a-267b9323a164"),
            traceContext: new MessageTraceContext(
                "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"),
            causationId: Guid.Parse("e01d0600-18dd-4573-9947-5c6a72eca8ab"),
            partitionKey: "orders/A-100");
    }

    private static DurableInboxRecord CreateInboxRecord()
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(
                Guid.Parse("4d2fa8ff-3375-4cde-a751-b3cc73da171e"),
                "fulfillment",
                "fulfillment.submit-order.v1"),
            DateTimeOffset.Parse("2026-06-04T00:00:02+00:00"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
