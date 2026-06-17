using System.Diagnostics;
using System.Diagnostics.Metrics;
using Bondstone.Messaging;

namespace Bondstone.Persistence;

internal static class BondstonePersistenceDiagnostics
{
    public const string ActivitySourceName = "Bondstone.Persistence";
    public const string OutboxDispatchActivityName = "bondstone.outbox.dispatch";
    public const string OutboxClaimedInstrumentName = "bondstone.outbox.claimed";
    public const string OutboxDispatchedInstrumentName = "bondstone.outbox.dispatched";
    public const string OutboxRetryScheduledInstrumentName = "bondstone.outbox.retry_scheduled";
    public const string OutboxTerminalFailedInstrumentName = "bondstone.outbox.terminal_failed";
    public const string OutboxStaleInstrumentName = "bondstone.outbox.stale";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(ActivitySourceName);
    private static readonly Counter<long> OutboxClaimedCounter = Meter.CreateCounter<long>(
        OutboxClaimedInstrumentName);
    private static readonly Counter<long> OutboxDispatchedCounter = Meter.CreateCounter<long>(
        OutboxDispatchedInstrumentName);
    private static readonly Counter<long> OutboxRetryScheduledCounter = Meter.CreateCounter<long>(
        OutboxRetryScheduledInstrumentName);
    private static readonly Counter<long> OutboxTerminalFailedCounter = Meter.CreateCounter<long>(
        OutboxTerminalFailedInstrumentName);
    private static readonly Counter<long> OutboxStaleCounter = Meter.CreateCounter<long>(
        OutboxStaleInstrumentName);

    public static class Tags
    {
        public const string ClaimedBy = "bondstone.outbox.claimed_by";
        public const string ClaimedCount = "bondstone.outbox.claimed_count";
        public const string DispatchedCount = "bondstone.outbox.dispatched_count";
        public const string MaxCount = "bondstone.outbox.max_count";
        public const string MessageId = "bondstone.message_id";
        public const string MessageKind = "bondstone.message_kind";
        public const string MessageType = "bondstone.message_type";
        public const string RetryScheduledCount = "bondstone.outbox.retry_scheduled_count";
        public const string SourceModule = "bondstone.source_module";
        public const string StaleCount = "bondstone.outbox.stale_count";
        public const string TargetModule = "bondstone.target_module";
        public const string TerminalFailedCount = "bondstone.outbox.terminal_failed_count";
    }

    public static void SetRecordTags(
        Activity activity,
        DurableOutboxRecord record)
    {
        DurableMessageEnvelope envelope = record.Envelope;
        activity.SetTag(Tags.MessageId, envelope.MessageId.ToString("D"));
        activity.SetTag(Tags.MessageKind, envelope.MessageKind.ToString());
        activity.SetTag(Tags.MessageType, envelope.MessageTypeName);
        activity.SetTag(Tags.SourceModule, envelope.SourceModule);
        activity.SetTag(Tags.TargetModule, envelope.TargetModule);
    }

    public static void SetDispatchResultTags(
        Activity activity,
        DurableOutboxDispatchResult result)
    {
        activity.SetTag(Tags.ClaimedCount, result.ClaimedCount);
        activity.SetTag(Tags.DispatchedCount, result.DispatchedCount);
        activity.SetTag(Tags.RetryScheduledCount, result.RetryScheduledCount);
        activity.SetTag(Tags.TerminalFailedCount, result.TerminalFailedCount);
        activity.SetTag(Tags.StaleCount, result.StaleCount);
    }

    public static void RecordOutboxClaimed(
        IReadOnlyList<DurableOutboxRecord> records)
    {
        foreach (DurableOutboxRecord record in records)
        {
            OutboxClaimedCounter.Add(1, CreateRecordTags(record));
        }
    }

    public static void RecordOutboxDispatched(
        DurableOutboxRecord record)
    {
        OutboxDispatchedCounter.Add(1, CreateRecordTags(record));
    }

    public static void RecordOutboxRetryScheduled(
        DurableOutboxRecord record)
    {
        OutboxRetryScheduledCounter.Add(1, CreateRecordTags(record));
    }

    public static void RecordOutboxTerminalFailed(
        DurableOutboxRecord record)
    {
        OutboxTerminalFailedCounter.Add(1, CreateRecordTags(record));
    }

    public static void RecordOutboxStale(
        DurableOutboxRecord record)
    {
        OutboxStaleCounter.Add(1, CreateRecordTags(record));
    }

    private static TagList CreateRecordTags(
        DurableOutboxRecord record)
    {
        DurableMessageEnvelope envelope = record.Envelope;
        var tags = new TagList
        {
            { Tags.MessageKind, envelope.MessageKind.ToString() },
            { Tags.SourceModule, envelope.SourceModule },
        };

        if (!string.IsNullOrWhiteSpace(envelope.TargetModule))
        {
            tags.Add(Tags.TargetModule, envelope.TargetModule);
        }

        return tags;
    }
}
