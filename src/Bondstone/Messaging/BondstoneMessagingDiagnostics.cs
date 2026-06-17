using System.Diagnostics;

namespace Bondstone.Messaging;

internal static class BondstoneMessagingDiagnostics
{
    public const string ActivitySourceName = "Bondstone.Modules";
    public const string CommandSendActivityName = "bondstone.command.send";
    public const string EventPublishActivityName = "bondstone.event.publish";
    public const string ModuleCommandReceiveActivityName = "bondstone.module_command.receive";
    public const string ModuleEventReceiveActivityName = "bondstone.module_event.receive";
    public const string OperationFinalizeActivityName = "bondstone.operation.finalize";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static class Tags
    {
        public const string HandlerIdentity = "bondstone.handler_identity";
        public const string MessageId = "bondstone.message_id";
        public const string MessageKind = "bondstone.message_kind";
        public const string MessageType = "bondstone.message_type";
        public const string Module = "bondstone.module";
        public const string OperationFinalized = "bondstone.operation_finalized";
        public const string OperationId = "bondstone.operation_id";
        public const string OperationStatus = "bondstone.operation_status";
        public const string PartitionKey = "bondstone.partition_key";
        public const string SourceModule = "bondstone.source_module";
        public const string TargetModule = "bondstone.target_module";
    }

    public static void SetEnvelopeTags(
        Activity activity,
        DurableMessageEnvelope envelope)
    {
        activity.SetTag(Tags.MessageId, envelope.MessageId.ToString("D"));
        activity.SetTag(Tags.MessageKind, envelope.MessageKind.ToString());
        activity.SetTag(Tags.MessageType, envelope.MessageTypeName);
        activity.SetTag(Tags.SourceModule, envelope.SourceModule);
        activity.SetTag(Tags.TargetModule, envelope.TargetModule);
        activity.SetTag(Tags.OperationId, envelope.DurableOperationId?.ToString("D"));
        activity.SetTag(Tags.PartitionKey, envelope.PartitionKey);
    }
}
