using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.Inbox;

public sealed class EntityFrameworkCoreDurableInboxInspectionStoreTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task FindUnprocessedAsync_WhenRowsExist_ReturnsFilteredUnprocessedRows()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        context.Set<InboxMessageEntity>().AddRange(
            InboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "sales",
                DateTimeOffset.Parse("2026-06-16T10:03:00+00:00"))),
            InboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "sales",
                DateTimeOffset.Parse("2026-06-16T10:01:00+00:00"),
                DateTimeOffset.Parse("2026-06-16T10:02:00+00:00"))),
            InboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "billing",
                DateTimeOffset.Parse("2026-06-16T10:02:00+00:00"))),
            InboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "sales",
                DateTimeOffset.Parse("2026-06-16T10:01:00+00:00"))),
            InboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "sales",
                DateTimeOffset.Parse("2026-06-16T10:05:00+00:00"))));
        await context.SaveChangesAsync();
        var store = new EntityFrameworkCoreDurableInboxInspectionStore<EntityFrameworkCoreTestDbContext>(
            context);

        IReadOnlyList<DurableInboxRecord> records = await store.FindUnprocessedAsync(
            maxCount: 2,
            receivedAtOrBeforeUtc: DateTimeOffset.Parse("2026-06-16T10:04:00+00:00"),
            moduleName: "sales");

        Assert.Collection(
            records,
            first =>
            {
                Assert.Equal(Guid.Parse("44444444-4444-4444-4444-444444444444"), first.Key.MessageId);
                Assert.False(first.IsProcessed);
            },
            second =>
            {
                Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), second.Key.MessageId);
                Assert.False(second.IsProcessed);
            });
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task FindUnprocessedAsync_WhenCutoffIsNotUtc_Throws()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableInboxInspectionStore<EntityFrameworkCoreTestDbContext>(
            context);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await store.FindUnprocessedAsync(
                receivedAtOrBeforeUtc: DateTimeOffset.Parse("2026-06-16T10:00:00+02:00")));
    }

    private static DurableInboxRecord CreateRecord(
        Guid messageId,
        string moduleName,
        DateTimeOffset receivedAtUtc,
        DateTimeOffset? processedAtUtc = null)
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(
                messageId,
                moduleName,
                $"{moduleName}.handler.v1"),
            receivedAtUtc,
            processedAtUtc);
    }
}
