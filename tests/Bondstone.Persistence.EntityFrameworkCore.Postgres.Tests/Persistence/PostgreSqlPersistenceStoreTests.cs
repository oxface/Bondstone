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
                resultPayload: """{"status":"ok"}""",
                diagnosticContext: new DurableOperationDiagnosticContext(
                    "fulfillment",
                    "fulfillment.order.reserve.v1",
                    "receive.fulfillment.order.reserve.v1"));

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
        Assert.Equal("fulfillment", entity.ModuleName);
        Assert.Equal("fulfillment.order.reserve.v1", entity.MessageTypeName);
        Assert.Equal("receive.fulfillment.order.reserve.v1", entity.HandlerIdentity);
    }
}
