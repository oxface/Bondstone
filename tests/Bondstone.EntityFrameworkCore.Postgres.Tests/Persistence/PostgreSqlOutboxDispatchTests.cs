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
    public async Task OutboxDispatchRecorder_WhenClaimedMessageIsDispatched_MarksDispatchedAndClearsClaim()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("1a08c3d5-4ea6-4f9d-9cf5-70c2d8029ce6");
        DateTimeOffset dispatchedAtUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");

        await WriteClaimedOutboxMessageAsync(messageId, "dispatcher-1");

        await using PostgreSqlTestDbContext context = CreateContext();
        var recorder = new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context);

        bool updated = await recorder.MarkDispatchedAsync(messageId, "dispatcher-1", dispatchedAtUtc);

        Assert.True(updated);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.Dispatched, entity.Status);
        Assert.Equal(dispatchedAtUtc, entity.DispatchedAtUtc);
        Assert.Null(entity.NextAttemptAtUtc);
        Assert.Null(entity.FailedAtUtc);
        Assert.Null(entity.FailureReason);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatchRecorder_WhenClaimedMessageFails_SchedulesRetryAndClearsClaim()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("2e597557-bacf-4ca5-9d89-5095cc6a62b6");
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");
        DateTimeOffset nextAttemptAtUtc = DateTimeOffset.Parse("2026-06-04T00:07:00+00:00");

        await WriteClaimedOutboxMessageAsync(messageId, "dispatcher-1");

        await using PostgreSqlTestDbContext context = CreateContext();
        var recorder = new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context);

        bool updated = await recorder.ScheduleRetryAsync(
            messageId,
            "dispatcher-1",
            " transport unavailable ",
            failedAtUtc,
            nextAttemptAtUtc);

        Assert.True(updated);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.Pending, entity.Status);
        Assert.Equal(failedAtUtc, entity.FailedAtUtc);
        Assert.Equal(nextAttemptAtUtc, entity.NextAttemptAtUtc);
        Assert.Equal("transport unavailable", entity.FailureReason);
        Assert.Null(entity.DispatchedAtUtc);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatchRecorder_WhenClaimedMessageIsDeadLettered_MarksDeadLetteredAndClearsClaim()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("0343d8d4-43b0-45e4-8d8c-b5cabf7e93d4");
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");

        await WriteClaimedOutboxMessageAsync(messageId, "dispatcher-1");

        await using PostgreSqlTestDbContext context = CreateContext();
        var recorder = new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context);

        bool updated = await recorder.MarkDeadLetteredAsync(
            messageId,
            "dispatcher-1",
            "poison message",
            failedAtUtc);

        Assert.True(updated);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.DeadLettered, entity.Status);
        Assert.Equal(failedAtUtc, entity.FailedAtUtc);
        Assert.Equal("poison message", entity.FailureReason);
        Assert.Null(entity.NextAttemptAtUtc);
        Assert.Null(entity.DispatchedAtUtc);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatchRecorder_WhenClaimIsOwnedByAnotherWorker_ReturnsFalse()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("db0d8d21-c98d-4d22-a96e-d796edaf31fe");

        await WriteClaimedOutboxMessageAsync(messageId, "dispatcher-1");

        await using PostgreSqlTestDbContext context = CreateContext();
        var recorder = new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context);

        bool updated = await recorder.MarkDispatchedAsync(
            messageId,
            "dispatcher-2",
            DateTimeOffset.Parse("2026-06-04T00:02:00+00:00"));

        Assert.False(updated);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.Processing, entity.Status);
        Assert.Equal("dispatcher-1", entity.ClaimedBy);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatchRecorder_WhenClaimLeaseExpired_ReturnsFalse()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("949c6c17-ad59-46ed-83dc-b59268f3ead0");

        await WriteClaimedOutboxMessageAsync(
            messageId,
            "dispatcher-1",
            DateTimeOffset.Parse("2026-06-04T00:01:59+00:00"));

        await using PostgreSqlTestDbContext context = CreateContext();
        var recorder = new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context);

        bool updated = await recorder.MarkDispatchedAsync(
            messageId,
            "dispatcher-1",
            DateTimeOffset.Parse("2026-06-04T00:02:00+00:00"));

        Assert.False(updated);
    }
}
