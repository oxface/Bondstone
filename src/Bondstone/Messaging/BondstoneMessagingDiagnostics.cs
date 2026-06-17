using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Bondstone.Messaging;

internal static class BondstoneMessagingDiagnostics
{
    public const string ActivitySourceName = "Bondstone.Modules";
    public const string CommandSendActivityName = "bondstone.command.send";
    public const string EventPublishActivityName = "bondstone.event.publish";
    public const string ModuleCommandReceiveActivityName = "bondstone.module_command.receive";
    public const string ModuleEventReceiveActivityName = "bondstone.module_event.receive";
    public const string OperationFinalizeActivityName = "bondstone.operation.finalize";
    public const string DirectReceiveHandledInstrumentName = "bondstone.direct_receive.handled";
    public const string DirectReceiveAlreadyProcessedInstrumentName = "bondstone.direct_receive.already_processed";
    public const string DirectReceiveAlreadyReceivedInstrumentName = "bondstone.direct_receive.already_received";
    public const string OperationFinalizedInstrumentName = "bondstone.operation.finalized";
    public const string OperationExpirationCandidatesInstrumentName = "bondstone.operation.expiration.candidates";
    public const string OperationExpirationFinalizedInstrumentName = "bondstone.operation.expiration.finalized";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(ActivitySourceName);
    private static readonly Counter<long> DirectReceiveHandledCounter = Meter.CreateCounter<long>(
        DirectReceiveHandledInstrumentName);
    private static readonly Counter<long> DirectReceiveAlreadyProcessedCounter = Meter.CreateCounter<long>(
        DirectReceiveAlreadyProcessedInstrumentName);
    private static readonly Counter<long> DirectReceiveAlreadyReceivedCounter = Meter.CreateCounter<long>(
        DirectReceiveAlreadyReceivedInstrumentName);
    private static readonly Counter<long> OperationFinalizedCounter = Meter.CreateCounter<long>(
        OperationFinalizedInstrumentName);
    private static readonly Counter<long> OperationExpirationCandidatesCounter = Meter.CreateCounter<long>(
        OperationExpirationCandidatesInstrumentName);
    private static readonly Counter<long> OperationExpirationFinalizedCounter = Meter.CreateCounter<long>(
        OperationExpirationFinalizedInstrumentName);

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

    public static void RecordDirectReceiveHandled(
        DurableMessageEnvelope envelope,
        string moduleName)
    {
        DirectReceiveHandledCounter.Add(1, CreateReceiveTags(envelope, moduleName));
    }

    public static void RecordDirectReceiveAlreadyProcessed(
        DurableMessageEnvelope envelope,
        string moduleName)
    {
        DirectReceiveAlreadyProcessedCounter.Add(1, CreateReceiveTags(envelope, moduleName));
    }

    public static void RecordDirectReceiveAlreadyReceived(
        DurableMessageEnvelope envelope,
        string moduleName)
    {
        DirectReceiveAlreadyReceivedCounter.Add(1, CreateReceiveTags(envelope, moduleName));
    }

    public static void RecordOperationFinalized(
        string moduleName,
        DurableOperationStatus terminalStatus)
    {
        OperationFinalizedCounter.Add(
            1,
            CreateOperationTags(moduleName, terminalStatus));
    }

    public static void RecordOperationExpirationCandidates(
        string moduleName,
        DurableOperationStatus terminalStatus,
        int count)
    {
        if (count <= 0)
        {
            return;
        }

        OperationExpirationCandidatesCounter.Add(
            count,
            CreateOperationTags(moduleName, terminalStatus));
    }

    public static void RecordOperationExpirationFinalized(
        string moduleName,
        DurableOperationStatus terminalStatus,
        int count)
    {
        if (count <= 0)
        {
            return;
        }

        OperationExpirationFinalizedCounter.Add(
            count,
            CreateOperationTags(moduleName, terminalStatus));
    }

    private static TagList CreateReceiveTags(
        DurableMessageEnvelope envelope,
        string moduleName)
    {
        var tags = new TagList
        {
            { Tags.Module, moduleName },
            { Tags.MessageKind, envelope.MessageKind.ToString() },
            { Tags.SourceModule, envelope.SourceModule },
        };

        if (!string.IsNullOrWhiteSpace(envelope.TargetModule))
        {
            tags.Add(Tags.TargetModule, envelope.TargetModule);
        }

        return tags;
    }

    private static TagList CreateOperationTags(
        string moduleName,
        DurableOperationStatus terminalStatus)
    {
        return new TagList
        {
            { Tags.Module, moduleName },
            { Tags.OperationStatus, terminalStatus.ToString() },
        };
    }
}
