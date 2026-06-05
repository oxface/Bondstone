using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Tests.Inbox;

public sealed class EntityFrameworkCoreDurableInboxStoreTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetAsync_WhenInboxMessageDoesNotExist_ReturnsNull()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableInboxStore<EntityFrameworkCoreTestDbContext>(context);

        DurableInboxRecord? record = await store.GetAsync(CreateKey());

        Assert.Null(record);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task AddAsync_WhenRecordIsValid_StagesInboxMessage()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableInboxStore<EntityFrameworkCoreTestDbContext>(context);
        DurableInboxRecord record = CreateRecord();

        await store.AddAsync(record);

        InboxMessageEntity entity = Assert.Single(
            context.ChangeTracker.Entries<InboxMessageEntity>().Select(static entry => entry.Entity));
        Assert.Equal(record.Key.MessageId, entity.MessageId);
        Assert.Equal(record.Key.ModuleName, entity.ModuleName);
        Assert.Equal(record.Key.HandlerIdentity, entity.HandlerIdentity);
        Assert.Equal(record.ReceivedAtUtc, entity.ReceivedAtUtc);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task AddAsync_WhenChangesAreSaved_PersistsInboxMessage()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableInboxStore<EntityFrameworkCoreTestDbContext>(context);
        DurableInboxRecord record = CreateRecord();

        await store.AddAsync(record);
        await context.SaveChangesAsync();

        DurableInboxRecord? mapped = await store.GetAsync(record.Key);
        Assert.Equal(record, mapped);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task MarkProcessedAsync_WhenRecordExists_StagesProcessedTimestamp()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableInboxStore<EntityFrameworkCoreTestDbContext>(context);
        DurableInboxRecord record = CreateRecord();
        DateTimeOffset processedAtUtc = DateTimeOffset.Parse("2026-06-04T00:01:00+00:00");

        await store.AddAsync(record);
        await context.SaveChangesAsync();
        await store.MarkProcessedAsync(record.Key, processedAtUtc);
        await context.SaveChangesAsync();

        DurableInboxRecord? mapped = await store.GetAsync(record.Key);
        Assert.NotNull(mapped);
        Assert.True(mapped.IsProcessed);
        Assert.Equal(processedAtUtc, mapped.ProcessedAtUtc);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task MarkProcessedAsync_WhenRecordDoesNotExist_Throws()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableInboxStore<EntityFrameworkCoreTestDbContext>(context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await store.MarkProcessedAsync(
                CreateKey(),
                DateTimeOffset.Parse("2026-06-04T00:01:00+00:00")));
    }

    private static DurableInboxRecord CreateRecord()
    {
        return new DurableInboxRecord(
            CreateKey(),
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
    }

    private static DurableInboxMessageKey CreateKey()
    {
        return new DurableInboxMessageKey(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "sales",
            "sales.customer.registered.v1");
    }
}
