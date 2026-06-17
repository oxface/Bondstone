using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.IncomingInbox;

public sealed class EntityFrameworkCoreDurableIncomingInboxInspectionStoreTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task FindAsync_WhenRowsExist_ReturnsFilteredRows()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        context.Set<IncomingInboxMessageEntity>().AddRange(
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "fulfillment",
                DurableIncomingInboxStatus.Pending,
                DateTimeOffset.Parse("2026-06-17T00:03:00+00:00"),
                sourceTransportName: "rabbitmq:orders")),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "fulfillment",
                DurableIncomingInboxStatus.Processed,
                DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
                sourceTransportName: "rabbitmq:orders",
                processedAtUtc: DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"))),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "billing",
                DurableIncomingInboxStatus.Pending,
                DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"),
                sourceTransportName: "rabbitmq:orders")),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "fulfillment",
                DurableIncomingInboxStatus.Pending,
                DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
                sourceTransportName: "rabbitmq:orders")),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "fulfillment",
                DurableIncomingInboxStatus.Pending,
                DateTimeOffset.Parse("2026-06-17T00:04:00+00:00"),
                sourceTransportName: "servicebus:orders")));
        await context.SaveChangesAsync();
        var store = new EntityFrameworkCoreDurableIncomingInboxInspectionStore<EntityFrameworkCoreTestDbContext>(
            context);

        IReadOnlyList<DurableIncomingInboxRecord> records = await store.FindAsync(
            DurableIncomingInboxStatus.Pending,
            maxCount: 2,
            ingestedAtOrBeforeUtc: DateTimeOffset.Parse("2026-06-17T00:03:00+00:00"),
            receiverModule: "fulfillment",
            sourceTransportName: "rabbitmq:orders");

        Assert.Collection(
            records,
            first => Assert.Equal(Guid.Parse("44444444-4444-4444-4444-444444444444"), first.Key.MessageId),
            second => Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), second.Key.MessageId));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task FindStaleProcessingAsync_WhenRowsExist_ReturnsExpiredProcessingClaims()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        context.Set<IncomingInboxMessageEntity>().AddRange(
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "fulfillment",
                DurableIncomingInboxStatus.Processing,
                DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
                sourceTransportName: "rabbitmq:orders",
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:03:00+00:00"))),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "fulfillment",
                DurableIncomingInboxStatus.Processing,
                DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
                sourceTransportName: "rabbitmq:orders",
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"))),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "billing",
                DurableIncomingInboxStatus.Processing,
                DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
                sourceTransportName: "rabbitmq:orders",
                claimedBy: "worker-2",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"))),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "fulfillment",
                DurableIncomingInboxStatus.Pending,
                DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
                sourceTransportName: "rabbitmq:orders")),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "fulfillment",
                DurableIncomingInboxStatus.Processing,
                DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
                sourceTransportName: "servicebus:orders",
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"))));
        await context.SaveChangesAsync();
        var store = new EntityFrameworkCoreDurableIncomingInboxInspectionStore<EntityFrameworkCoreTestDbContext>(
            context);

        IReadOnlyList<DurableIncomingInboxRecord> records = await store.FindStaleProcessingAsync(
            DateTimeOffset.Parse("2026-06-17T00:03:00+00:00"),
            maxCount: 2,
            receiverModule: "fulfillment",
            sourceTransportName: "rabbitmq:orders");

        Assert.Collection(
            records,
            first =>
            {
                Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), first.Key.MessageId);
                Assert.Equal(DurableIncomingInboxStatus.Processing, first.State.Status);
            },
            second =>
            {
                Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), second.Key.MessageId);
                Assert.Equal(DurableIncomingInboxStatus.Processing, second.State.Status);
            });
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task FindTerminalFailedAsync_WhenRowsExist_ReturnsFilteredTerminalFailures()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        context.Set<IncomingInboxMessageEntity>().AddRange(
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "fulfillment",
                DurableIncomingInboxStatus.TerminalFailed,
                DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
                sourceTransportName: "rabbitmq:orders",
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:03:00+00:00"))),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "fulfillment",
                DurableIncomingInboxStatus.RetryScheduled,
                DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
                sourceTransportName: "rabbitmq:orders",
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
                nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00"))),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "billing",
                DurableIncomingInboxStatus.TerminalFailed,
                DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
                sourceTransportName: "rabbitmq:orders",
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"))),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "fulfillment",
                DurableIncomingInboxStatus.TerminalFailed,
                DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
                sourceTransportName: "rabbitmq:orders",
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"))),
            IncomingInboxMessageEntity.FromRecord(CreateRecord(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "fulfillment",
                DurableIncomingInboxStatus.TerminalFailed,
                DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
                sourceTransportName: "servicebus:orders",
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"))));
        await context.SaveChangesAsync();
        var store = new EntityFrameworkCoreDurableIncomingInboxInspectionStore<EntityFrameworkCoreTestDbContext>(
            context);

        IReadOnlyList<DurableIncomingInboxRecord> records = await store.FindTerminalFailedAsync(
            maxCount: 2,
            failedAtOrBeforeUtc: DateTimeOffset.Parse("2026-06-17T00:03:00+00:00"),
            receiverModule: "fulfillment",
            sourceTransportName: "rabbitmq:orders");

        Assert.Collection(
            records,
            first =>
            {
                Assert.Equal(Guid.Parse("44444444-4444-4444-4444-444444444444"), first.Key.MessageId);
                Assert.Equal(DurableIncomingInboxStatus.TerminalFailed, first.State.Status);
            },
            second =>
            {
                Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), second.Key.MessageId);
                Assert.Equal(DurableIncomingInboxStatus.TerminalFailed, second.State.Status);
            });
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task InspectionQueries_WhenCutoffIsNotUtc_Throw()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableIncomingInboxInspectionStore<EntityFrameworkCoreTestDbContext>(
            context);
        DateTimeOffset nonUtc = DateTimeOffset.Parse("2026-06-17T00:00:00+02:00");

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await store.FindAsync(ingestedAtOrBeforeUtc: nonUtc));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await store.FindStaleProcessingAsync(nonUtc));
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await store.FindTerminalFailedAsync(failedAtOrBeforeUtc: nonUtc));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task FindAsync_WhenIncomingInboxIsNotMapped_ThrowsSetupError()
    {
        await using MissingIncomingInboxMappingDbContext context = MissingIncomingInboxMappingDbContext.Create();
        var store =
            new EntityFrameworkCoreDurableIncomingInboxInspectionStore<MissingIncomingInboxMappingDbContext>(context);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await store.FindAsync());

        Assert.Contains("missing the Bondstone EF Core incoming inbox mapping", exception.Message);
        Assert.Contains("ApplyBondstoneIncomingInbox()", exception.Message);
    }

    private static DurableIncomingInboxRecord CreateRecord(
        Guid messageId,
        string receiverModule,
        DurableIncomingInboxStatus status,
        DateTimeOffset ingestedAtUtc,
        string? sourceTransportName = null,
        DateTimeOffset? processedAtUtc = null,
        DateTimeOffset? failedAtUtc = null,
        DateTimeOffset? nextAttemptAtUtc = null,
        string? claimedBy = null,
        DateTimeOffset? claimedUntilUtc = null)
    {
        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Command,
            $"{receiverModule}.message.v1",
            "ordering",
            receiverModule,
            "{}",
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"));
        var state = new DurableIncomingInboxState(
            status,
            attemptCount: status == DurableIncomingInboxStatus.Pending
                ? 0
                : 1,
            nextAttemptAtUtc,
            processedAtUtc,
            failedAtUtc,
            status is DurableIncomingInboxStatus.RetryScheduled or DurableIncomingInboxStatus.TerminalFailed
                ? "receive failed"
                : null,
            claimedBy,
            claimedUntilUtc);

        return new DurableIncomingInboxRecord(
            DurableIncomingInboxKey.ForCommandHandler(
                envelope.MessageId,
                receiverModule,
                $"{receiverModule}.handler.v1"),
            envelope,
            ingestedAtUtc,
            state,
            sourceTransportName);
    }
}
