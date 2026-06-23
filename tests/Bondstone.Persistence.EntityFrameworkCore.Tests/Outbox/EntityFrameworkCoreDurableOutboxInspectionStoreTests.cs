using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.Outbox;

public sealed class EntityFrameworkCoreDurableOutboxInspectionStoreTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task FindTerminalFailedAsync_WhenRowsExist_ReturnsFilteredTerminalRows()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        context.Set<OutboxMessageEntity>().AddRange(
            CreateEntity(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "sales",
                DurableOutboxStatus.TerminalFailed,
                failedAtUtc: DateTimeOffset.Parse("2026-06-16T10:03:00+00:00")),
            CreateEntity(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "sales",
                DurableOutboxStatus.Failed,
                failedAtUtc: DateTimeOffset.Parse("2026-06-16T10:01:00+00:00")),
            CreateEntity(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "billing",
                DurableOutboxStatus.TerminalFailed,
                failedAtUtc: DateTimeOffset.Parse("2026-06-16T10:02:00+00:00")),
            CreateEntity(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "sales",
                DurableOutboxStatus.TerminalFailed,
                failedAtUtc: DateTimeOffset.Parse("2026-06-16T10:01:00+00:00")),
            CreateEntity(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "sales",
                DurableOutboxStatus.TerminalFailed,
                failedAtUtc: DateTimeOffset.Parse("2026-06-16T10:05:00+00:00")));
        await context.SaveChangesAsync();
        var store = new EntityFrameworkCoreDurableOutboxInspectionStore<EntityFrameworkCoreTestDbContext>(
            context);

        IReadOnlyList<DurableOutboxRecord> records = await store.FindTerminalFailedAsync(
            maxCount: 2,
            failedAtOrBeforeUtc: DateTimeOffset.Parse("2026-06-16T10:04:00+00:00"),
            sourceModuleName: "sales");

        Assert.Collection(
            records,
            first =>
            {
                Assert.Equal(Guid.Parse("44444444-4444-4444-4444-444444444444"), first.Envelope.MessageId);
                Assert.Equal(DurableOutboxStatus.TerminalFailed, first.DispatchState.Status);
            },
            second =>
            {
                Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), second.Envelope.MessageId);
                Assert.Equal(DurableOutboxStatus.TerminalFailed, second.DispatchState.Status);
            });
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task FindTerminalFailedAsync_WhenCutoffIsNotUtc_Throws()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableOutboxInspectionStore<EntityFrameworkCoreTestDbContext>(
            context);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await store.FindTerminalFailedAsync(
                failedAtOrBeforeUtc: DateTimeOffset.Parse("2026-06-16T10:00:00+02:00")));
    }

    private static OutboxMessageEntity CreateEntity(
        Guid messageId,
        string sourceModule,
        DurableOutboxStatus status,
        DateTimeOffset failedAtUtc)
    {
        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Command,
            $"{sourceModule}.test.v1",
            sourceModule,
            "target",
            "{}",
            DateTimeOffset.Parse("2026-06-16T10:00:00+00:00"));
        var dispatchState = new DurableOutboxDispatchState(
            status,
            attemptCount: 3,
            failedAtUtc: failedAtUtc,
            failureReason: status == DurableOutboxStatus.TerminalFailed
                ? "terminal failure"
                : "dispatch failed");

        return OutboxMessageEntity.FromRecord(new DurableOutboxRecord(
            envelope,
            DateTimeOffset.Parse("2026-06-16T10:00:01+00:00"),
            dispatchState));
    }
}
