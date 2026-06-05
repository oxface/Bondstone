using Bondstone.Messaging;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableOutboxDispatcherTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenRecordsAreSent_RecordsDispatchedOutcomes()
    {
        DurableOutboxRecord first = CreateRecord(Guid.Parse("40d76ea9-188f-4ed4-bd1d-a6bc65a58101"));
        DurableOutboxRecord second = CreateRecord(Guid.Parse("7fc91fbb-7f01-4a9c-9e2c-cac014cb29ce"));
        var claimer = new FakeClaimer([first, second]);
        var leaseRenewer = new FakeLeaseRenewer();
        var transport = new FakeTransport();
        var recorder = new FakeDispatchRecorder();
        DurableOutboxDispatcher dispatcher = CreateDispatcher(claimer, leaseRenewer, transport, recorder: recorder);

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            " dispatcher-1 ",
            TimeSpan.FromMinutes(5),
            maxCount: 10);

        Assert.Equal(2, result.ClaimedCount);
        Assert.Equal(2, result.DispatchedCount);
        Assert.Equal(0, result.StaleCount);
        Assert.Equal(["dispatcher-1"], claimer.ClaimedByValues);
        Assert.Equal([first.Envelope.MessageId, second.Envelope.MessageId], transport.SentMessageIds);
        Assert.Equal([first.Envelope.MessageId, second.Envelope.MessageId], recorder.DispatchedMessageIds);
        Assert.Equal([first.Envelope.MessageId, second.Envelope.MessageId], leaseRenewer.RenewedMessageIds);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenLeaseCannotBeRenewed_SkipsTransportSend()
    {
        DurableOutboxRecord record = CreateRecord(Guid.Parse("523eb5d5-f157-4a4d-abcf-d228d23703e9"));
        var leaseRenewer = new FakeLeaseRenewer { RenewResult = false };
        var transport = new FakeTransport();
        DurableOutboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([record]),
            leaseRenewer,
            transport);

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(1, result.ClaimedCount);
        Assert.Equal(1, result.StaleCount);
        Assert.Empty(transport.SentMessageIds);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenTransportFailsAndPolicyRetries_SchedulesRetry()
    {
        DurableOutboxRecord record = CreateRecord(Guid.Parse("79fffc4d-545d-46e4-b8df-e4e8b992ace1"));
        var transport = new FakeTransport { Exception = new InvalidOperationException("transport unavailable") };
        var recorder = new FakeDispatchRecorder();
        DurableOutboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([record]),
            new FakeLeaseRenewer(),
            transport,
            failurePolicy: new DurableOutboxFailurePolicy(maxAttempts: 5, retryDelays: [TimeSpan.FromSeconds(10)]),
            recorder: recorder);

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(0, result.DispatchedCount);
        Assert.Equal(1, result.RetryScheduledCount);
        Assert.Equal(record.Envelope.MessageId, Assert.Single(recorder.RetryMessageIds));
        Assert.Contains("transport unavailable", Assert.Single(recorder.FailureReasons));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenTransportFailsAndMaxAttemptsReached_MarksDeadLettered()
    {
        DurableOutboxRecord record = CreateRecord(
            Guid.Parse("4b6fdcfb-c8ad-4f50-b243-82cc3055fef7"),
            attemptCount: 5);
        var transport = new FakeTransport { Exception = new InvalidOperationException("poison") };
        var recorder = new FakeDispatchRecorder();
        DurableOutboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([record]),
            new FakeLeaseRenewer(),
            transport,
            failurePolicy: new DurableOutboxFailurePolicy(maxAttempts: 5),
            recorder: recorder);

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(1, result.DeadLetteredCount);
        Assert.Equal(record.Envelope.MessageId, Assert.Single(recorder.DeadLetteredMessageIds));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenOutcomeCannotBeRecorded_CountsStale()
    {
        DurableOutboxRecord record = CreateRecord(Guid.Parse("c01f12b7-8a0b-4cfa-94d8-997191e39d16"));
        var recorder = new FakeDispatchRecorder { RecordResult = false };
        DurableOutboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([record]),
            new FakeLeaseRenewer(),
            new FakeTransport(),
            recorder: recorder);

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(1, result.StaleCount);
        Assert.Equal(0, result.DispatchedCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenCancellationIsRequested_RethrowsCancellation()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        DurableOutboxRecord record = CreateRecord(Guid.Parse("b23f236b-722c-4a91-b03e-75e658e8f3d9"));
        var transport = new FakeTransport { Exception = new OperationCanceledException(cancellationTokenSource.Token) };
        DurableOutboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([record]),
            new FakeLeaseRenewer(),
            transport);

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await dispatcher.DispatchAsync(
                "dispatcher-1",
                TimeSpan.FromMinutes(5),
                cancellationToken: cancellationTokenSource.Token));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenClaimedByIsBlank_Throws()
    {
        DurableOutboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([]),
            new FakeLeaseRenewer(),
            new FakeTransport());

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await dispatcher.DispatchAsync(" ", TimeSpan.FromMinutes(5)));
    }

    private static DurableOutboxDispatcher CreateDispatcher(
        FakeClaimer claimer,
        FakeLeaseRenewer leaseRenewer,
        FakeTransport transport,
        IDurableOutboxFailurePolicy? failurePolicy = null,
        FakeDispatchRecorder? recorder = null)
    {
        return new DurableOutboxDispatcher(
            claimer,
            leaseRenewer,
            transport,
            failurePolicy ?? new DurableOutboxFailurePolicy(),
            recorder ?? new FakeDispatchRecorder(),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-05T00:10:00+00:00")));
    }

    private static DurableOutboxRecord CreateRecord(
        Guid messageId,
        int attemptCount = 1)
    {
        var dispatchState = new DurableOutboxDispatchState(
            DurableOutboxStatus.Processing,
            attemptCount,
            claimedBy: "dispatcher-1",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-05T00:15:00+00:00"));

        return new DurableOutboxRecord(
            new DurableMessageEnvelope(
                messageId,
                MessageKind.Command,
                "sales.customer.register.v1",
                "sales",
                "billing",
                "{}",
                DateTimeOffset.Parse("2026-06-05T00:00:00+00:00")),
            DateTimeOffset.Parse("2026-06-05T00:00:01+00:00"),
            dispatchState);
    }

    private sealed class FakeClaimer(IReadOnlyList<DurableOutboxRecord> records)
        : IDurableOutboxClaimer
    {
        public List<string> ClaimedByValues { get; } = [];

        public ValueTask<IReadOnlyList<DurableOutboxRecord>> ClaimAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken cancellationToken = default)
        {
            ClaimedByValues.Add(claimedBy);
            return ValueTask.FromResult(records);
        }
    }

    private sealed class FakeLeaseRenewer : IDurableOutboxLeaseRenewer
    {
        public bool RenewResult { get; init; } = true;

        public List<Guid> RenewedMessageIds { get; } = [];

        public ValueTask<bool> RenewAsync(
            Guid messageId,
            string claimedBy,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            RenewedMessageIds.Add(messageId);
            return ValueTask.FromResult(RenewResult);
        }
    }

    private sealed class FakeTransport : IDurableOutboxTransport
    {
        public Exception? Exception { get; init; }

        public List<Guid> SentMessageIds { get; } = [];

        public ValueTask SendAsync(
            DurableOutboxRecord record,
            CancellationToken cancellationToken = default)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            SentMessageIds.Add(record.Envelope.MessageId);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeDispatchRecorder : IDurableOutboxDispatchRecorder
    {
        public bool RecordResult { get; init; } = true;

        public List<Guid> DispatchedMessageIds { get; } = [];

        public List<Guid> RetryMessageIds { get; } = [];

        public List<Guid> DeadLetteredMessageIds { get; } = [];

        public List<string> FailureReasons { get; } = [];

        public ValueTask<bool> MarkDispatchedAsync(
            Guid messageId,
            string claimedBy,
            DateTimeOffset dispatchedAtUtc,
            CancellationToken cancellationToken = default)
        {
            DispatchedMessageIds.Add(messageId);
            return ValueTask.FromResult(RecordResult);
        }

        public ValueTask<bool> ScheduleRetryAsync(
            Guid messageId,
            string claimedBy,
            string failureReason,
            DateTimeOffset failedAtUtc,
            DateTimeOffset nextAttemptAtUtc,
            CancellationToken cancellationToken = default)
        {
            RetryMessageIds.Add(messageId);
            FailureReasons.Add(failureReason);
            return ValueTask.FromResult(RecordResult);
        }

        public ValueTask<bool> MarkDeadLetteredAsync(
            Guid messageId,
            string claimedBy,
            string failureReason,
            DateTimeOffset failedAtUtc,
            CancellationToken cancellationToken = default)
        {
            DeadLetteredMessageIds.Add(messageId);
            FailureReasons.Add(failureReason);
            return ValueTask.FromResult(RecordResult);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
