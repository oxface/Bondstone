using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;

public sealed class IncomingInboxMessageEntity
{
    private IncomingInboxMessageEntity(
        Guid messageId,
        MessageKind messageKind,
        string messageTypeName,
        string sourceModule,
        string? targetModule,
        Guid? durableOperationId,
        string? traceParent,
        string? traceState,
        string? traceBaggage,
        Guid? causationId,
        string? partitionKey,
        string payload,
        string? metadata,
        DateTimeOffset createdAtUtc,
        string receiverModule,
        string handlerIdentity,
        string? sourceTransportName,
        DateTimeOffset ingestedAtUtc,
        DurableIncomingInboxStatus status,
        int attemptCount,
        DateTimeOffset? nextAttemptAtUtc,
        DateTimeOffset? processedAtUtc,
        DateTimeOffset? failedAtUtc,
        string? failureReason,
        string? claimedBy,
        DateTimeOffset? claimedUntilUtc)
    {
        MessageId = messageId;
        MessageKind = messageKind;
        MessageTypeName = messageTypeName;
        SourceModule = sourceModule;
        TargetModule = targetModule;
        DurableOperationId = durableOperationId;
        TraceParent = traceParent;
        TraceState = traceState;
        TraceBaggage = traceBaggage;
        CausationId = causationId;
        PartitionKey = partitionKey;
        Payload = payload;
        Metadata = metadata;
        CreatedAtUtc = createdAtUtc;
        ReceiverModule = receiverModule;
        HandlerIdentity = handlerIdentity;
        SourceTransportName = sourceTransportName;
        IngestedAtUtc = ingestedAtUtc;
        Status = status;
        AttemptCount = attemptCount;
        NextAttemptAtUtc = nextAttemptAtUtc;
        ProcessedAtUtc = processedAtUtc;
        FailedAtUtc = failedAtUtc;
        FailureReason = failureReason;
        ClaimedBy = claimedBy;
        ClaimedUntilUtc = claimedUntilUtc;
    }

    private IncomingInboxMessageEntity()
    {
    }

    public Guid MessageId { get; private set; }

    public MessageKind MessageKind { get; private set; }

    public string MessageTypeName { get; private set; } = string.Empty;

    public string SourceModule { get; private set; } = string.Empty;

    public string? TargetModule { get; private set; }

    public Guid? DurableOperationId { get; private set; }

    public string? TraceParent { get; private set; }

    public string? TraceState { get; private set; }

    public string? TraceBaggage { get; private set; }

    public Guid? CausationId { get; private set; }

    public string? PartitionKey { get; private set; }

    public string Payload { get; private set; } = string.Empty;

    public string? Metadata { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string ReceiverModule { get; private set; } = string.Empty;

    public string HandlerIdentity { get; private set; } = string.Empty;

    public string? SourceTransportName { get; private set; }

    public DateTimeOffset IngestedAtUtc { get; private set; }

    public DurableIncomingInboxStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public DateTimeOffset? FailedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public string? ClaimedBy { get; private set; }

    public DateTimeOffset? ClaimedUntilUtc { get; private set; }

    public static IncomingInboxMessageEntity FromRecord(
        DurableIncomingInboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        DurableIncomingInboxState state = record.State;

        return new IncomingInboxMessageEntity(
            envelope.MessageId,
            envelope.MessageKind,
            envelope.MessageTypeName,
            envelope.SourceModule,
            envelope.TargetModule,
            envelope.DurableOperationId,
            envelope.TraceContext?.TraceParent,
            envelope.TraceContext?.TraceState,
            envelope.TraceContext?.Baggage,
            envelope.CausationId,
            envelope.PartitionKey,
            envelope.Payload,
            envelope.Metadata,
            envelope.CreatedAtUtc,
            record.ReceiverModule,
            record.HandlerIdentity,
            record.SourceTransportName,
            record.IngestedAtUtc,
            state.Status,
            state.AttemptCount,
            state.NextAttemptAtUtc,
            state.ProcessedAtUtc,
            state.FailedAtUtc,
            state.FailureReason,
            state.ClaimedBy,
            state.ClaimedUntilUtc);
    }

    public DurableIncomingInboxRecord ToRecord()
    {
        MessageTraceContext? traceContext = TraceParent is null
            ? null
            : new MessageTraceContext(TraceParent, TraceState, TraceBaggage);

        var envelope = new DurableMessageEnvelope(
            MessageId,
            MessageKind,
            MessageTypeName,
            SourceModule,
            TargetModule,
            Payload,
            CreatedAtUtc,
            DurableOperationId,
            traceContext,
            CausationId,
            PartitionKey,
            Metadata);

        var key = new DurableIncomingInboxKey(
            MessageId,
            ReceiverModule,
            HandlerIdentity);

        var state = new DurableIncomingInboxState(
            Status,
            AttemptCount,
            NextAttemptAtUtc,
            ProcessedAtUtc,
            FailedAtUtc,
            FailureReason,
            ClaimedBy,
            ClaimedUntilUtc);

        return new DurableIncomingInboxRecord(
            key,
            envelope,
            IngestedAtUtc,
            state,
            SourceTransportName);
    }
}
