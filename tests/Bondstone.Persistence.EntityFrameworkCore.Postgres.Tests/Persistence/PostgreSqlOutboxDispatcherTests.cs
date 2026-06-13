using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatcher_WhenTransportSucceeds_DispatchesClaimedMessages()
    {
        await ResetDatabaseAsync();
        Guid firstMessageId = Guid.Parse("5da74cce-19e0-4892-81a3-df0a94b0f91c");
        Guid secondMessageId = Guid.Parse("7bc36a97-2b9f-422b-a363-aaf7e16ca480");
        DateTimeOffset dispatcherTimeUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");

        await WriteOutboxMessagesAsync(
            (firstMessageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")),
            (secondMessageId, DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));

        RecordingTransport transport = new();
        await using PostgreSqlTestDbContext context = CreateContext();
        DurableOutboxDispatcher dispatcher = CreateOutboxDispatcher(
            context,
            transport,
            new FixedTimeProvider(dispatcherTimeUtc));

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            " dispatcher-1 ",
            TimeSpan.FromMinutes(5),
            maxCount: 10);

        Assert.Equal(2, result.ClaimedCount);
        Assert.Equal(2, result.DispatchedCount);
        Assert.Equal(0, result.RetryScheduledCount);
        Assert.Equal(0, result.TerminalFailedCount);
        Assert.Equal(0, result.StaleCount);
        Assert.Equal(2, result.CompletedCount);
        Assert.Equal([firstMessageId, secondMessageId], transport.MessageIds);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        List<OutboxMessageEntity> entities = await verificationContext
            .Set<OutboxMessageEntity>()
            .OrderBy(entity => entity.StoredAtUtc)
            .ToListAsync();

        Assert.Collection(
            entities,
            entity => AssertDispatched(entity, firstMessageId, dispatcherTimeUtc),
            entity => AssertDispatched(entity, secondMessageId, dispatcherTimeUtc));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatcher_WhenTransportFailsBeforeMaxAttempts_SchedulesRetry()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("e2bb2a7d-870b-4828-b234-059df4756e4d");
        DateTimeOffset dispatcherTimeUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");

        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

        await using PostgreSqlTestDbContext context = CreateContext();
        DurableOutboxDispatcher dispatcher = CreateOutboxDispatcher(
            context,
            new ThrowingTransport(new InvalidOperationException("transport unavailable")),
            new FixedTimeProvider(dispatcherTimeUtc),
            new DurableOutboxFailurePolicy(
                maxAttempts: 3,
                retryDelays: [TimeSpan.FromMinutes(2)]));

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(1, result.ClaimedCount);
        Assert.Equal(0, result.DispatchedCount);
        Assert.Equal(1, result.RetryScheduledCount);
        Assert.Equal(0, result.TerminalFailedCount);
        Assert.Equal(0, result.StaleCount);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.Pending, entity.Status);
        Assert.Equal(1, entity.AttemptCount);
        Assert.Equal(dispatcherTimeUtc, entity.FailedAtUtc);
        Assert.Equal(dispatcherTimeUtc.AddMinutes(2), entity.NextAttemptAtUtc);
        Assert.Contains("transport unavailable", entity.FailureReason);
        Assert.Null(entity.DispatchedAtUtc);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatcher_WhenTransportFailsAtMaxAttempts_TerminalFailsMessage()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("adf6d869-11a7-4c9d-a9e4-515ce4a76743");
        DateTimeOffset dispatcherTimeUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");

        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

        await using PostgreSqlTestDbContext context = CreateContext();
        DurableOutboxDispatcher dispatcher = CreateOutboxDispatcher(
            context,
            new ThrowingTransport(new InvalidOperationException("poison message")),
            new FixedTimeProvider(dispatcherTimeUtc),
            new DurableOutboxFailurePolicy(maxAttempts: 1));

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(1, result.ClaimedCount);
        Assert.Equal(0, result.DispatchedCount);
        Assert.Equal(0, result.RetryScheduledCount);
        Assert.Equal(1, result.TerminalFailedCount);
        Assert.Equal(0, result.StaleCount);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.TerminalFailed, entity.Status);
        Assert.Equal(1, entity.AttemptCount);
        Assert.Equal(dispatcherTimeUtc, entity.FailedAtUtc);
        Assert.Contains("poison message", entity.FailureReason);
        Assert.Null(entity.NextAttemptAtUtc);
        Assert.Null(entity.DispatchedAtUtc);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    private static DurableOutboxDispatcher CreateOutboxDispatcher(
        PostgreSqlTestDbContext context,
        IDurableOutboxTransport transport,
        TimeProvider timeProvider,
        IDurableOutboxFailurePolicy? failurePolicy = null)
    {
        return new DurableOutboxDispatcher(
            new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(context, timeProvider),
            new PostgreSqlDurableOutboxLeaseRenewer<PostgreSqlTestDbContext>(context, timeProvider),
            transport,
            failurePolicy ?? new DurableOutboxFailurePolicy(),
            new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context),
            timeProvider);
    }

    private static void AssertDispatched(
        OutboxMessageEntity entity,
        Guid messageId,
        DateTimeOffset dispatchedAtUtc)
    {
        Assert.Equal(messageId, entity.MessageId);
        Assert.Equal(DurableOutboxStatus.Dispatched, entity.Status);
        Assert.Equal(1, entity.AttemptCount);
        Assert.Equal(dispatchedAtUtc, entity.DispatchedAtUtc);
        Assert.Null(entity.NextAttemptAtUtc);
        Assert.Null(entity.FailedAtUtc);
        Assert.Null(entity.FailureReason);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    private sealed class RecordingTransport : IDurableOutboxTransport
    {
        private readonly List<Guid> _messageIds = [];

        public IReadOnlyList<Guid> MessageIds => _messageIds;

        public ValueTask SendAsync(
            DurableOutboxRecord record,
            CancellationToken ct = default)
        {
            _messageIds.Add(record.Envelope.MessageId);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingTransport(Exception exception) : IDurableOutboxTransport
    {
        public ValueTask SendAsync(
            DurableOutboxRecord record,
            CancellationToken ct = default)
        {
            throw exception;
        }
    }
}
