using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests
{
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
    public async Task InboxRegistrar_WhenRecordDoesNotExist_RegistersInboxRecord()
    {
        await ResetDatabaseAsync();
        DurableInboxRecord record = CreateInboxRecord();

        await using PostgreSqlTestDbContext context = CreateContext();
        var registrar = new PostgreSqlDurableInboxRegistrar<PostgreSqlTestDbContext>(context);

        DurableInboxRegistrationResult result = await registrar.RegisterAsync(record);

        Assert.Equal(DurableInboxRegistrationStatus.Registered, result.Status);
        Assert.True(result.IsRegistered);
        Assert.False(result.IsDuplicate);
        Assert.Equal(record, result.Record);
        Assert.Equal(1, await context.Set<InboxMessageEntity>().CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InboxRegistrar_WhenRecordAlreadyExists_ReturnsAlreadyReceived()
    {
        await ResetDatabaseAsync();
        DurableInboxRecord record = CreateInboxRecord();

        await using (PostgreSqlTestDbContext setupContext = CreateContext())
        {
            var registrar = new PostgreSqlDurableInboxRegistrar<PostgreSqlTestDbContext>(setupContext);
            await registrar.RegisterAsync(record);
        }

        await using PostgreSqlTestDbContext context = CreateContext();
        var duplicateRegistrar = new PostgreSqlDurableInboxRegistrar<PostgreSqlTestDbContext>(context);

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
    public async Task InboxRegistrar_WhenRecordAlreadyProcessed_ReturnsAlreadyProcessed()
    {
        await ResetDatabaseAsync();
        DurableInboxRecord record = CreateInboxRecord();
        DateTimeOffset processedAtUtc = DateTimeOffset.Parse("2026-06-04T00:03:00+00:00");

        await using (PostgreSqlTestDbContext setupContext = CreateContext())
        {
            var registrar = new PostgreSqlDurableInboxRegistrar<PostgreSqlTestDbContext>(setupContext);
            await registrar.RegisterAsync(record);
            var store = new EntityFrameworkCoreDurableInboxStore<PostgreSqlTestDbContext>(setupContext);
            await store.MarkProcessedAsync(record.Key, processedAtUtc);
            await setupContext.SaveChangesAsync();
        }

        await using PostgreSqlTestDbContext context = CreateContext();
        var duplicateRegistrar = new PostgreSqlDurableInboxRegistrar<PostgreSqlTestDbContext>(context);

        DurableInboxRegistrationResult result = await duplicateRegistrar.RegisterAsync(record);

        Assert.Equal(DurableInboxRegistrationStatus.AlreadyProcessed, result.Status);
        Assert.True(result.IsDuplicate);
        Assert.Equal(processedAtUtc, result.Record.ProcessedAtUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task InboxRegistrar_WhenDuplicateInsideTransaction_DoesNotAbortTransaction()
    {
        await ResetDatabaseAsync();
        DurableInboxRecord record = CreateInboxRecord();
        DurableMessageEnvelope envelope = CreateEnvelope(Guid.Parse("ec35d211-1ed2-4fd5-98a1-383114a9fb81"));

        await using (PostgreSqlTestDbContext setupContext = CreateContext())
        {
            var registrar = new PostgreSqlDurableInboxRegistrar<PostgreSqlTestDbContext>(setupContext);
            await registrar.RegisterAsync(record);
        }

        await using (PostgreSqlTestDbContext context = CreateContext())
        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync())
        {
            var registrar = new PostgreSqlDurableInboxRegistrar<PostgreSqlTestDbContext>(context);
            DurableInboxRegistrationResult result = await registrar.RegisterAsync(record);

            Assert.Equal(DurableInboxRegistrationStatus.AlreadyReceived, result.Status);

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
}
