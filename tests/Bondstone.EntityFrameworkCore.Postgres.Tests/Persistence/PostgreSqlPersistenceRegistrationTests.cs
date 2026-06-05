using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Inbox;
using Bondstone.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AddBondstonePostgreSqlPersistence_WhenResolved_UsesPostgreSqlStores()
    {
        await ResetDatabaseAsync();
        var services = new ServiceCollection();
        services.AddBondstonePostgreSqlPersistence<PostgreSqlTestDbContext>(_fixture.ConnectionString);

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
        DateTimeOffset dispatchedAtUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(claimTimeUtc));
        services.AddBondstonePostgreSqlPersistence<PostgreSqlSchemaTestDbContext>(
            _fixture.ConnectionString,
            schema: PostgreSqlSchemaTestDbContext.BondstoneSchema);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IDurableOutboxWriter writer = scope.ServiceProvider.GetRequiredService<IDurableOutboxWriter>();
        IDurableInboxRegistrar inboxRegistrar = scope.ServiceProvider.GetRequiredService<IDurableInboxRegistrar>();
        IDurableInboxHandlerExecutor inboxExecutor =
            scope.ServiceProvider.GetRequiredService<IDurableInboxHandlerExecutor>();
        IEntityFrameworkCorePersistenceScope persistenceScope =
            scope.ServiceProvider.GetRequiredService<IEntityFrameworkCorePersistenceScope>();
        IDurableOutboxClaimer claimer = scope.ServiceProvider.GetRequiredService<IDurableOutboxClaimer>();
        IDurableOutboxLeaseRenewer leaseRenewer =
            scope.ServiceProvider.GetRequiredService<IDurableOutboxLeaseRenewer>();
        PostgreSqlSchemaTestDbContext context =
            scope.ServiceProvider.GetRequiredService<PostgreSqlSchemaTestDbContext>();
        IDurableOutboxDispatchRecorder dispatchStore =
            scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatchRecorder>();
        DurableMessageEnvelope envelope = CreateEnvelope(Guid.Parse("55f93286-f946-481c-9651-2834dd2a253d"));
        DurableInboxRecord inboxRecord = CreateInboxRecord();
        DurableInboxRecord handledInboxRecord = CreateInboxRecord(
            Guid.Parse("0f159c80-5333-4f45-b4dd-a227f6913d7a"));
        var handlerCalls = 0;

        DurableInboxRegistrationResult inboxResult = await inboxRegistrar.RegisterAsync(inboxRecord);
        DurableInboxHandleResult handleResult = await persistenceScope.ExecuteAsync(
            async (persistence, ct) => await inboxExecutor.HandleOnceAsync(
                handledInboxRecord,
                _ =>
                {
                    handlerCalls++;
                    return ValueTask.CompletedTask;
                },
                persistence.SaveChangesAsync,
                ct));
        await writer.WriteAsync(envelope);
        await context.SaveChangesAsync();

        Assert.Equal(DurableInboxRegistrationStatus.Registered, inboxResult.Status);
        Assert.Equal(DurableInboxHandleStatus.Handled, handleResult.Status);
        Assert.Equal(1, handlerCalls);

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        DurableOutboxRecord record = Assert.Single(records);
        Assert.Equal(envelope.MessageId, record.Envelope.MessageId);
        Assert.Equal(DurableOutboxStatus.Processing, record.DispatchState.Status);
        Assert.Equal("dispatcher-1", record.DispatchState.ClaimedBy);
        Assert.Equal(claimTimeUtc.AddMinutes(5), record.DispatchState.ClaimedUntilUtc);

        bool renewed = await leaseRenewer.RenewAsync(
            envelope.MessageId,
            "dispatcher-1",
            TimeSpan.FromMinutes(10));

        Assert.True(renewed);

        bool dispatched = await dispatchStore.MarkDispatchedAsync(
            envelope.MessageId,
            "dispatcher-1",
            dispatchedAtUtc);

        Assert.True(dispatched);
    }
}
