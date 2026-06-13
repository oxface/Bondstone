using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Persistence.EntityFrameworkCore.Outbox;

public sealed class OutboxMessageEntity
{
    private OutboxMessageEntity(
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
        DateTimeOffset storedAtUtc,
        DurableOutboxStatus status,
        int attemptCount,
        DateTimeOffset? nextAttemptAtUtc,
        DateTimeOffset? dispatchedAtUtc,
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
        StoredAtUtc = storedAtUtc;
        Status = status;
        AttemptCount = attemptCount;
        NextAttemptAtUtc = nextAttemptAtUtc;
        DispatchedAtUtc = dispatchedAtUtc;
        FailedAtUtc = failedAtUtc;
        FailureReason = failureReason;
        ClaimedBy = claimedBy;
        ClaimedUntilUtc = claimedUntilUtc;
    }

    private OutboxMessageEntity()
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

    public DateTimeOffset StoredAtUtc { get; private set; }

    public DurableOutboxStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset? NextAttemptAtUtc { get; private set; }

    public DateTimeOffset? DispatchedAtUtc { get; private set; }

    public DateTimeOffset? FailedAtUtc { get; private set; }

    public string? FailureReason { get; private set; }

    public string? ClaimedBy { get; private set; }

    public DateTimeOffset? ClaimedUntilUtc { get; private set; }

    public static OutboxMessageEntity FromRecord(DurableOutboxRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        DurableMessageEnvelope envelope = record.Envelope;
        DurableOutboxDispatchState dispatchState = record.DispatchState;

        return new OutboxMessageEntity(
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
            record.StoredAtUtc,
            dispatchState.Status,
            dispatchState.AttemptCount,
            dispatchState.NextAttemptAtUtc,
            dispatchState.DispatchedAtUtc,
            dispatchState.FailedAtUtc,
            dispatchState.FailureReason,
            dispatchState.ClaimedBy,
            dispatchState.ClaimedUntilUtc);
    }

    public DurableOutboxRecord ToRecord()
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

        var dispatchState = new DurableOutboxDispatchState(
            Status,
            AttemptCount,
            NextAttemptAtUtc,
            DispatchedAtUtc,
            FailedAtUtc,
            FailureReason,
            ClaimedBy,
            ClaimedUntilUtc);

        return new DurableOutboxRecord(envelope, StoredAtUtc, dispatchState);
    }
}
