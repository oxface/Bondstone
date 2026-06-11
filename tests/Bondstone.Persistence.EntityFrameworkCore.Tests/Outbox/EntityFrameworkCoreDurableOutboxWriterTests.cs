using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkCoreDurableOutboxWriterTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task WriteAsync_WhenEnvelopeIsValid_StagesOutboxMessage()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var timeProvider = new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00"));
        var writer = new EntityFrameworkCoreDurableOutboxWriter<EntityFrameworkCoreTestDbContext>(
            context,
            timeProvider);
        DurableMessageEnvelope envelope = CreateEnvelope();

        await writer.WriteAsync(envelope);

        OutboxMessageEntity entity = Assert.Single(
            context.ChangeTracker.Entries<OutboxMessageEntity>().Select(static entry => entry.Entity));
        Assert.Equal(envelope.MessageId, entity.MessageId);
        Assert.Equal(timeProvider.GetUtcNow(), entity.StoredAtUtc);
        Assert.Equal(DurableOutboxStatus.Pending, entity.Status);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task WriteAsync_WhenChangesAreSaved_PersistsOutboxMessage()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var writer = new EntityFrameworkCoreDurableOutboxWriter<EntityFrameworkCoreTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));
        DurableMessageEnvelope envelope = CreateEnvelope();

        await writer.WriteAsync(envelope);
        await context.SaveChangesAsync();

        OutboxMessageEntity? entity = await context
            .Set<OutboxMessageEntity>()
            .FindAsync(envelope.MessageId);

        Assert.NotNull(entity);
        Assert.Equal(envelope.MessageTypeName, entity.MessageTypeName);
    }

    private static DurableMessageEnvelope CreateEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "sales.customer.register.v1",
            "sales",
            "billing",
            "{}",
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
