using System.Diagnostics;
using System.Diagnostics.Metrics;
using Bondstone.Messaging;

namespace Bondstone.Persistence;

internal static class IncomingInboxProcessingDiagnostics
{
    public const string ActivitySourceName = BondstoneMessagingDiagnostics.ActivitySourceName;
    public const string ProcessActivityName = "bondstone.incoming_inbox.process";
    public const string ProcessMessageActivityName = "bondstone.incoming_inbox.process.message";
    public const string ClaimedInstrumentName = "bondstone.incoming_inbox.claimed";
    public const string ProcessedInstrumentName = "bondstone.incoming_inbox.processed";
    public const string RetryScheduledInstrumentName = "bondstone.incoming_inbox.retry_scheduled";
    public const string TerminalFailedInstrumentName = "bondstone.incoming_inbox.terminal_failed";
    public const string StaleInstrumentName = "bondstone.incoming_inbox.stale";

    public static readonly ActivitySource ActivitySource = BondstoneMessagingDiagnostics.ActivitySource;
    private static readonly Meter Meter = new(ActivitySourceName);
    private static readonly Counter<long> ClaimedCounter = Meter.CreateCounter<long>(
        ClaimedInstrumentName);
    private static readonly Counter<long> ProcessedCounter = Meter.CreateCounter<long>(
        ProcessedInstrumentName);
    private static readonly Counter<long> RetryScheduledCounter = Meter.CreateCounter<long>(
        RetryScheduledInstrumentName);
    private static readonly Counter<long> TerminalFailedCounter = Meter.CreateCounter<long>(
        TerminalFailedInstrumentName);
    private static readonly Counter<long> StaleCounter = Meter.CreateCounter<long>(
        StaleInstrumentName);

    public static class Tags
    {
        public const string ClaimedCount = "bondstone.incoming_inbox.claimed_count";
        public const string MaxCount = "bondstone.incoming_inbox.max_count";
        public const string MessageKind = "bondstone.message_kind";
        public const string MessageType = "bondstone.message_type";
        public const string ProcessedCount = "bondstone.incoming_inbox.processed_count";
        public const string ReceiverModule = "bondstone.module";
        public const string RetryScheduledCount = "bondstone.incoming_inbox.retry_scheduled_count";
        public const string SourceModule = "bondstone.source_module";
        public const string SourceTransport = "bondstone.source_transport";
        public const string StaleCount = "bondstone.incoming_inbox.stale_count";
        public const string TargetModule = "bondstone.target_module";
        public const string TerminalFailedCount = "bondstone.incoming_inbox.terminal_failed_count";
    }

    public static void SetRecordTags(
        Activity activity,
        DurableIncomingInboxRecord record)
    {
        DurableMessageEnvelope envelope = record.Envelope;
        activity.SetTag(Tags.MessageKind, envelope.MessageKind.ToString());
        activity.SetTag(Tags.MessageType, envelope.MessageTypeName);
        activity.SetTag(Tags.SourceModule, envelope.SourceModule);
        activity.SetTag(Tags.TargetModule, envelope.TargetModule);
        activity.SetTag(Tags.ReceiverModule, record.ReceiverModule);
        activity.SetTag(Tags.SourceTransport, record.SourceTransportName);
    }

    public static void SetProcessingResultTags(
        Activity activity,
        DurableIncomingInboxProcessingResult result)
    {
        activity.SetTag(Tags.ClaimedCount, result.ClaimedCount);
        activity.SetTag(Tags.ProcessedCount, result.ProcessedCount);
        activity.SetTag(Tags.RetryScheduledCount, result.RetryScheduledCount);
        activity.SetTag(Tags.TerminalFailedCount, result.TerminalFailedCount);
        activity.SetTag(Tags.StaleCount, result.StaleCount);
    }

    public static void RecordClaimed(
        IReadOnlyList<DurableIncomingInboxRecord> records)
    {
        foreach (DurableIncomingInboxRecord record in records)
        {
            ClaimedCounter.Add(1, CreateRecordTags(record));
        }
    }

    public static void RecordProcessed(
        DurableIncomingInboxRecord record)
    {
        ProcessedCounter.Add(1, CreateRecordTags(record));
    }

    public static void RecordRetryScheduled(
        DurableIncomingInboxRecord record)
    {
        RetryScheduledCounter.Add(1, CreateRecordTags(record));
    }

    public static void RecordTerminalFailed(
        DurableIncomingInboxRecord record)
    {
        TerminalFailedCounter.Add(1, CreateRecordTags(record));
    }

    public static void RecordStale(
        DurableIncomingInboxRecord record)
    {
        StaleCounter.Add(1, CreateRecordTags(record));
    }

    private static TagList CreateRecordTags(
        DurableIncomingInboxRecord record)
    {
        DurableMessageEnvelope envelope = record.Envelope;
        var tags = new TagList
        {
            { Tags.MessageKind, envelope.MessageKind.ToString() },
            { Tags.SourceModule, envelope.SourceModule },
            { Tags.ReceiverModule, record.ReceiverModule },
        };

        if (!string.IsNullOrWhiteSpace(envelope.TargetModule))
        {
            tags.Add(Tags.TargetModule, envelope.TargetModule);
        }

        if (!string.IsNullOrWhiteSpace(record.SourceTransportName))
        {
            tags.Add(Tags.SourceTransport, record.SourceTransportName);
        }

        return tags;
    }
}
