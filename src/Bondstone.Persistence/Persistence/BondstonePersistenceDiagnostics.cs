using System.Diagnostics;
using Bondstone.Messaging;

namespace Bondstone.Persistence;

internal static class BondstonePersistenceDiagnostics
{
    public const string ActivitySourceName = "Bondstone.Persistence";
    public const string OutboxDispatchActivityName = "bondstone.outbox.dispatch";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

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
}
