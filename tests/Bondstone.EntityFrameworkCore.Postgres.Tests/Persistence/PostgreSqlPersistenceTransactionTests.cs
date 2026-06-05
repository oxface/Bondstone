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
    public async Task PersistenceScope_WhenOperationCompletes_CommitsSavedChanges()
    {
        await ResetDatabaseAsync();
        DurableMessageEnvelope envelope = CreateEnvelope(Guid.Parse("fbe66c4e-d448-4c9a-9495-5cc432fefba3"));

        await using (PostgreSqlTestDbContext context = CreateContext())
        {
            var scope = new EntityFrameworkCorePersistenceScope<PostgreSqlTestDbContext>(context);
            var writer = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                context,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

            bool result = await scope.ExecuteAsync<bool>(
                async (persistence, ct) =>
                {
                    await writer.WriteAsync(envelope, ct);
                    await persistence.SaveChangesAsync(ct);
                    return true;
                });

            Assert.True(result);
        }

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        Assert.Equal(envelope.MessageId, await verificationContext
            .Set<OutboxMessageEntity>()
            .Select(static entity => entity.MessageId)
            .SingleAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PersistenceScope_WhenOperationThrows_RollsBackSavedChanges()
    {
        await ResetDatabaseAsync();
        DurableMessageEnvelope envelope = CreateEnvelope(Guid.Parse("455288b4-39fc-45bc-934e-b31bb5b39b8b"));

        await using PostgreSqlTestDbContext context = CreateContext();
        var scope = new EntityFrameworkCorePersistenceScope<PostgreSqlTestDbContext>(context);
        var writer = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ExecuteAsync(
                async (persistence, ct) =>
                {
                    await writer.WriteAsync(envelope, ct);
                    await persistence.SaveChangesAsync(ct);
                    throw new InvalidOperationException("Operation failed.");
                }));

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        Assert.Empty(await verificationContext.Set<OutboxMessageEntity>().ToArrayAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task PersistenceScope_WhenTransactionAlreadyExists_DoesNotCommitOwnedTransaction()
    {
        await ResetDatabaseAsync();
        DurableMessageEnvelope envelope = CreateEnvelope(Guid.Parse("104d1894-c4f8-408e-b933-aa092d7c7684"));

        await using (PostgreSqlTestDbContext context = CreateContext())
        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync())
        {
            var scope = new EntityFrameworkCorePersistenceScope<PostgreSqlTestDbContext>(context);
            var writer = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                context,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

            await scope.ExecuteAsync(
                async (persistence, ct) =>
                {
                    await writer.WriteAsync(envelope, ct);
                    await persistence.SaveChangesAsync(ct);
                });

            await transaction.RollbackAsync();
        }

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        Assert.Empty(await verificationContext.Set<OutboxMessageEntity>().ToArrayAsync());
    }
}
