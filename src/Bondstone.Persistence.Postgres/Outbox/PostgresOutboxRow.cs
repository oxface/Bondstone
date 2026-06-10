using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Persistence.Postgres.Outbox;

internal sealed class PostgresOutboxRow
{
    public Guid MessageId { get; init; }

    public string MessageKind { get; init; } = string.Empty;

    public string MessageTypeName { get; init; } = string.Empty;

    public string SourceModule { get; init; } = string.Empty;

    public string? TargetModule { get; init; }

    public Guid? DurableOperationId { get; init; }

    public string? TraceParent { get; init; }

    public string? TraceState { get; init; }

    public string? TraceBaggage { get; init; }

    public Guid? CausationId { get; init; }

    public string? PartitionKey { get; init; }

    public string Payload { get; init; } = string.Empty;

    public string? Metadata { get; init; }

    public DateTimeOffset CreatedAtUtc { get; init; }

    public DateTimeOffset StoredAtUtc { get; init; }

    public string Status { get; init; } = string.Empty;

    public int AttemptCount { get; init; }

    public DateTimeOffset? NextAttemptAtUtc { get; init; }

    public DateTimeOffset? DispatchedAtUtc { get; init; }

    public DateTimeOffset? FailedAtUtc { get; init; }

    public string? FailureReason { get; init; }

    public string? ClaimedBy { get; init; }

    public DateTimeOffset? ClaimedUntilUtc { get; init; }

    public DurableOutboxRecord ToRecord()
    {
        MessageTraceContext? traceContext = TraceParent is null
            ? null
            : new MessageTraceContext(TraceParent, TraceState, TraceBaggage);
        var envelope = new DurableMessageEnvelope(
            MessageId,
            Enum.Parse<MessageKind>(MessageKind),
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
            Enum.Parse<DurableOutboxStatus>(Status),
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
