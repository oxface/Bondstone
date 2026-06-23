using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableIncomingInboxDispatcherTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessAsync_WhenClaimedRowsSucceed_InvokesReceivePipelinesAndRecordsProcessed()
    {
        DurableIncomingInboxRecord command = CreateCommandRecord(
            Guid.Parse("31d04358-931b-4530-9635-2a3376ae3fc5"));
        DurableIncomingInboxRecord integrationEvent = CreateEventRecord(
            Guid.Parse("a73a9313-c91e-4f2a-853f-03887661df59"));
        var claimer = new FakeClaimer([command, integrationEvent]);
        var commandReceiver = new FakeCommandReceivePipeline();
        var eventReceiver = new FakeEventReceivePipeline();
        var recorder = new FakeOutcomeRecorder();
        DurableIncomingInboxDispatcher dispatcher = CreateDispatcher(
            claimer,
            commandReceiver,
            eventReceiver,
            recorder);

        DurableIncomingInboxProcessingResult result = await dispatcher.ProcessAsync(
            " receiver-1 ",
            TimeSpan.FromMinutes(5),
            maxCount: 10);

        Assert.Equal(2, result.ClaimedCount);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(0, result.StaleCount);
        Assert.Equal(["receiver-1"], claimer.ClaimedByValues);
        Assert.Equal([command.Envelope.MessageId], commandReceiver.MessageIds);
        Assert.Equal(
            [(integrationEvent.Envelope.MessageId, integrationEvent.ReceiverModule, integrationEvent.HandlerIdentity)],
            eventReceiver.Received);
        Assert.Equal([command.Key, integrationEvent.Key], recorder.ProcessedKeys);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessAsync_WhenHandlerFailsBeforeMaxAttempts_SchedulesRetry()
    {
        DurableIncomingInboxRecord record = CreateCommandRecord(
            Guid.Parse("9162b8b5-d94d-47d2-962b-a3e98065cd58"));
        var commandReceiver = new FakeCommandReceivePipeline
        {
            Exception = new InvalidOperationException("handler unavailable"),
        };
        var recorder = new FakeOutcomeRecorder();
        DurableIncomingInboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([record]),
            commandReceiver,
            new FakeEventReceivePipeline(),
            recorder,
            new DurableIncomingInboxFailurePolicy(
                new DurableIncomingInboxProcessingOptions(
                    maxAttempts: 5,
                    retryDelays: [TimeSpan.FromSeconds(10)])));

        DurableIncomingInboxProcessingResult result = await dispatcher.ProcessAsync(
            "receiver-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(1, result.RetryScheduledCount);
        Assert.Equal(record.Key, Assert.Single(recorder.RetryKeys));
        Assert.Contains("handler unavailable", Assert.Single(recorder.FailureReasons));
        Assert.Equal(
            DateTimeOffset.Parse("2026-06-05T00:10:10+00:00"),
            Assert.Single(recorder.NextAttemptValues));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessAsync_WhenHandlerFailsAtMaxAttempts_MarksTerminalFailed()
    {
        DurableIncomingInboxRecord record = CreateCommandRecord(
            Guid.Parse("3e78d436-02ad-40f9-9cf1-05fd03686fd8"),
            attemptCount: 5);
        var commandReceiver = new FakeCommandReceivePipeline
        {
            Exception = new InvalidOperationException("poison"),
        };
        var recorder = new FakeOutcomeRecorder();
        DurableIncomingInboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([record]),
            commandReceiver,
            new FakeEventReceivePipeline(),
            recorder,
            new DurableIncomingInboxFailurePolicy(
                new DurableIncomingInboxProcessingOptions(maxAttempts: 5)));

        DurableIncomingInboxProcessingResult result = await dispatcher.ProcessAsync(
            "receiver-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(0, result.RetryScheduledCount);
        Assert.Equal(1, result.TerminalFailedCount);
        Assert.Equal(record.Key, Assert.Single(recorder.TerminalFailedKeys));
        Assert.Contains("poison", Assert.Single(recorder.FailureReasons));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessAsync_WhenProcessedOutcomeCannotBeRecorded_CountsStaleClaim()
    {
        DurableIncomingInboxRecord record = CreateCommandRecord(
            Guid.Parse("df8e9a7c-4e18-489f-b439-004c1ca88310"));
        var recorder = new FakeOutcomeRecorder { RecordResult = false };
        DurableIncomingInboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([record]),
            new FakeCommandReceivePipeline(),
            new FakeEventReceivePipeline(),
            recorder);

        DurableIncomingInboxProcessingResult result = await dispatcher.ProcessAsync(
            "receiver-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(1, result.StaleCount);
        Assert.Equal(record.Key, Assert.Single(recorder.ProcessedKeys));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessAsync_WhenOneRecordFails_ContinuesProcessingRemainingBatch()
    {
        DurableIncomingInboxRecord retry = CreateCommandRecord(
            Guid.Parse("0cc68e76-e095-4c55-af5d-c0d4e3fa2cb6"));
        DurableIncomingInboxRecord processed = CreateCommandRecord(
            Guid.Parse("a12ff66f-9ce8-4e1e-806f-47d950f68e4a"));
        var commandReceiver = new FakeCommandReceivePipeline();
        commandReceiver.ExceptionsByMessageId[retry.Envelope.MessageId] =
            new InvalidOperationException("first failed");
        var recorder = new FakeOutcomeRecorder();
        DurableIncomingInboxDispatcher dispatcher = CreateDispatcher(
            new FakeClaimer([retry, processed]),
            commandReceiver,
            new FakeEventReceivePipeline(),
            recorder);

        DurableIncomingInboxProcessingResult result = await dispatcher.ProcessAsync(
            "receiver-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(2, result.ClaimedCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.RetryScheduledCount);
        Assert.Equal([retry.Envelope.MessageId, processed.Envelope.MessageId], commandReceiver.MessageIds);
        Assert.Equal(retry.Key, Assert.Single(recorder.RetryKeys));
        Assert.Equal(processed.Key, Assert.Single(recorder.ProcessedKeys));
    }

    private static DurableIncomingInboxDispatcher CreateDispatcher(
        FakeClaimer claimer,
        FakeCommandReceivePipeline commandReceivePipeline,
        FakeEventReceivePipeline eventReceivePipeline,
        FakeOutcomeRecorder recorder,
        IDurableIncomingInboxFailurePolicy? failurePolicy = null)
    {
        return new DurableIncomingInboxDispatcher(
            claimer,
            commandReceivePipeline,
            eventReceivePipeline,
            recorder,
            failurePolicy ?? new DurableIncomingInboxFailurePolicy(),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-05T00:10:00+00:00")));
    }

    private static DurableIncomingInboxRecord CreateCommandRecord(
        Guid messageId,
        int attemptCount = 1)
    {
        DurableMessageEnvelope envelope = CreateEnvelope(
            messageId,
            MessageKind.Command,
            targetModule: "billing");
        var key = DurableIncomingInboxKey.ForCommandHandler(
            messageId,
            "billing",
            "billing.register-customer");

        return CreateRecord(key, envelope, attemptCount);
    }

    private static DurableIncomingInboxRecord CreateEventRecord(
        Guid messageId,
        int attemptCount = 1)
    {
        DurableMessageEnvelope envelope = CreateEnvelope(
            messageId,
            MessageKind.Event,
            targetModule: null);
        var key = DurableIncomingInboxKey.ForEventSubscriber(
            messageId,
            "fulfillment",
            "fulfillment.customer-registered");

        return CreateRecord(key, envelope, attemptCount);
    }

    private static DurableIncomingInboxRecord CreateRecord(
        DurableIncomingInboxKey key,
        DurableMessageEnvelope envelope,
        int attemptCount)
    {
        var state = new DurableIncomingInboxState(
            DurableIncomingInboxStatus.Processing,
            attemptCount,
            claimedBy: "receiver-1",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-05T00:15:00+00:00"));

        return new DurableIncomingInboxRecord(
            key,
            envelope,
            DateTimeOffset.Parse("2026-06-05T00:00:01+00:00"),
            state,
            "rabbitmq:orders");
    }

    private static DurableMessageEnvelope CreateEnvelope(
        Guid messageId,
        MessageKind kind,
        string? targetModule)
    {
        return new DurableMessageEnvelope(
            messageId,
            kind,
            "sales.customer.register.v1",
            "sales",
            targetModule,
            "{}",
            DateTimeOffset.Parse("2026-06-05T00:00:00+00:00"));
    }

    private static DurableInboxHandleResult CreateInboxResult(
        DurableIncomingInboxRecord record)
    {
        var inboxRecord = new DurableInboxRecord(
            new DurableInboxMessageKey(
                record.Key.MessageId,
                record.ReceiverModule,
                record.HandlerIdentity),
            DateTimeOffset.Parse("2026-06-05T00:10:00+00:00"),
            DateTimeOffset.Parse("2026-06-05T00:10:00+00:00"));

        return new DurableInboxHandleResult(
            DurableInboxHandleStatus.Handled,
            inboxRecord);
    }

    private sealed class FakeClaimer(IReadOnlyList<DurableIncomingInboxRecord> records)
        : IDurableIncomingInboxClaimer
    {
        public List<string> ClaimedByValues { get; } = [];

        public ValueTask<IReadOnlyList<DurableIncomingInboxRecord>> ClaimAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken ct = default)
        {
            ClaimedByValues.Add(claimedBy);
            return ValueTask.FromResult(records);
        }
    }

    private sealed class FakeCommandReceivePipeline : IModuleCommandReceivePipeline
    {
        public Exception? Exception { get; init; }

        public Dictionary<Guid, Exception> ExceptionsByMessageId { get; } = [];

        public List<Guid> MessageIds { get; } = [];

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            MessageIds.Add(envelope.MessageId);

            if (ExceptionsByMessageId.TryGetValue(envelope.MessageId, out Exception? exception))
            {
                throw exception;
            }

            if (Exception is not null)
            {
                throw Exception;
            }

            DurableIncomingInboxRecord record = CreateCommandRecord(envelope.MessageId);
            return ValueTask.FromResult(CreateInboxResult(record));
        }
    }

    private sealed class FakeEventReceivePipeline : IModuleEventReceivePipeline
    {
        public List<(Guid MessageId, string SubscriberModule, string SubscriberIdentity)> Received { get; } = [];

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            Received.Add((envelope.MessageId, subscriberModule, subscriberIdentity));
            DurableIncomingInboxRecord record = CreateEventRecord(envelope.MessageId);
            return ValueTask.FromResult(CreateInboxResult(record));
        }
    }

    private sealed class FakeOutcomeRecorder : IDurableIncomingInboxOutcomeRecorder
    {
        public bool RecordResult { get; init; } = true;

        public List<DurableIncomingInboxKey> ProcessedKeys { get; } = [];

        public List<DurableIncomingInboxKey> RetryKeys { get; } = [];

        public List<DurableIncomingInboxKey> TerminalFailedKeys { get; } = [];

        public List<string> FailureReasons { get; } = [];

        public List<DateTimeOffset> NextAttemptValues { get; } = [];

        public ValueTask<bool> MarkProcessedAsync(
            DurableIncomingInboxKey key,
            string claimedBy,
            DateTimeOffset processedAtUtc,
            CancellationToken ct = default)
        {
            ProcessedKeys.Add(key);
            return ValueTask.FromResult(RecordResult);
        }

        public ValueTask<bool> ScheduleRetryAsync(
            DurableIncomingInboxKey key,
            string claimedBy,
            string failureReason,
            DateTimeOffset failedAtUtc,
            DateTimeOffset nextAttemptAtUtc,
            CancellationToken ct = default)
        {
            RetryKeys.Add(key);
            FailureReasons.Add(failureReason);
            NextAttemptValues.Add(nextAttemptAtUtc);
            return ValueTask.FromResult(RecordResult);
        }

        public ValueTask<bool> MarkTerminalFailedAsync(
            DurableIncomingInboxKey key,
            string claimedBy,
            string failureReason,
            DateTimeOffset failedAtUtc,
            CancellationToken ct = default)
        {
            TerminalFailedKeys.Add(key);
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
