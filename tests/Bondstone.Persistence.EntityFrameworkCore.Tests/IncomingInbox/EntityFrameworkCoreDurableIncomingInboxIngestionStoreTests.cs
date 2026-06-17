using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.IncomingInbox;

public sealed class EntityFrameworkCoreDurableIncomingInboxIngestionStoreTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task IngestAsync_WhenRecordIsNew_StagesIncomingInboxMessage()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableIncomingInboxIngestionStore<EntityFrameworkCoreTestDbContext>(
            context);
        DurableIncomingInboxRecord record = CreateRecord();

        DurableIncomingInboxIngestionResult result = await store.IngestAsync(record);

        Assert.Equal(DurableIncomingInboxIngestionStatus.Ingested, result.Status);
        Assert.Same(record, result.Record);
        IncomingInboxMessageEntity entity = Assert.Single(
            context.ChangeTracker.Entries<IncomingInboxMessageEntity>().Select(static entry => entry.Entity));
        Assert.Equal(record.Key.MessageId, entity.MessageId);
        Assert.Equal(record.ReceiverModule, entity.ReceiverModule);
        Assert.Equal(record.HandlerIdentity, entity.HandlerIdentity);
        Assert.Equal(DurableIncomingInboxStatus.Pending, entity.Status);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task IngestAsync_WhenRecordAlreadyExists_ReturnsExistingRecord()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableIncomingInboxIngestionStore<EntityFrameworkCoreTestDbContext>(
            context);
        DurableIncomingInboxRecord record = CreateRecord(sourceTransportName: "rabbitmq:orders");
        DurableIncomingInboxRecord duplicate = CreateRecord(sourceTransportName: "servicebus:orders");

        await store.IngestAsync(record);
        await context.SaveChangesAsync();

        DurableIncomingInboxIngestionResult result = await store.IngestAsync(duplicate);

        Assert.Equal(DurableIncomingInboxIngestionStatus.AlreadyIngested, result.Status);
        Assert.True(result.WasAlreadyIngested);
        Assert.Equal(record, result.Record);
        Assert.Single(await context.Set<IncomingInboxMessageEntity>().ToListAsync());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task IngestAsync_WhenExistingRecordIsTrackedBeforeSave_ReturnsAlreadyIngested()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableIncomingInboxIngestionStore<EntityFrameworkCoreTestDbContext>(
            context);
        DurableIncomingInboxRecord record = CreateRecord();

        await store.IngestAsync(record);
        DurableIncomingInboxIngestionResult result = await store.IngestAsync(record);

        Assert.Equal(DurableIncomingInboxIngestionStatus.AlreadyIngested, result.Status);
        Assert.Equal(record, result.Record);
        Assert.Single(context.ChangeTracker.Entries<IncomingInboxMessageEntity>());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task IngestAsync_WhenNewRecordIsNotPending_Throws()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableIncomingInboxIngestionStore<EntityFrameworkCoreTestDbContext>(
            context);
        DurableIncomingInboxRecord record = CreateRecord(
            state: new DurableIncomingInboxState(
                DurableIncomingInboxStatus.RetryScheduled,
                attemptCount: 1,
                nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00"),
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:04:00+00:00"),
                failureReason: "receive failed"));

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await store.IngestAsync(record));

        Assert.Equal("record", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task IngestAsync_WhenIncomingInboxIsNotMapped_ThrowsSetupError()
    {
        await using MissingIncomingInboxMappingDbContext context = MissingIncomingInboxMappingDbContext.Create();
        var store = new EntityFrameworkCoreDurableIncomingInboxIngestionStore<MissingIncomingInboxMappingDbContext>(
            context);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await store.IngestAsync(CreateRecord()));

        Assert.Contains("missing the Bondstone EF Core incoming inbox mapping", exception.Message);
        Assert.Contains("ApplyBondstoneIncomingInbox()", exception.Message);
    }

    private static DurableIncomingInboxRecord CreateRecord(
        DurableIncomingInboxState? state = null,
        string? sourceTransportName = null)
    {
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "ordering.reserve-inventory.v1",
            "ordering",
            "fulfillment",
            "{}",
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"));

        return new DurableIncomingInboxRecord(
            DurableIncomingInboxKey.ForCommandHandler(
                envelope.MessageId,
                "fulfillment",
                "fulfillment.reserve-inventory.v1"),
            envelope,
            DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
            state,
            sourceTransportName);
    }
}
