namespace Bondstone.Messaging;

public sealed record DurableOperationResult<TResult>
{
    public DurableOperationResult(
        Guid durableOperationId,
        DurableOperationStatus? status,
        DateTimeOffset? updatedAtUtc,
        TResult? result = default,
        bool hasResult = false,
        string? failureReason = null)
        : this(
            durableOperationId,
            status,
            updatedAtUtc,
            result,
            hasResult,
            failureReason,
            deserializationFailure: null)
    {
    }

    internal DurableOperationResult(
        Guid durableOperationId,
        DurableOperationStatus? status,
        DateTimeOffset? updatedAtUtc,
        TResult? result,
        bool hasResult,
        string? failureReason,
        DurableOperationResultDeserializationFailure? deserializationFailure)
    {
        if (durableOperationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Durable operation id must not be empty.",
                nameof(durableOperationId));
        }

        if (status is DurableOperationStatus operationStatus
            && !Enum.IsDefined(operationStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Operation status is not supported.");
        }

        if (deserializationFailure is not null)
        {
            if (deserializationFailure.DurableOperationId != durableOperationId)
            {
                throw new ArgumentException(
                    "Deserialization failure operation id must match the durable operation id.",
                    nameof(deserializationFailure));
            }

            if (status != DurableOperationStatus.Completed)
            {
                throw new ArgumentException(
                    "Deserialization failure can only be reported for a completed durable operation.",
                    nameof(deserializationFailure));
            }
        }

        DurableOperationId = durableOperationId;
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
        Result = result;
        HasResult = deserializationFailure is null && hasResult;
        FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? null
            : failureReason;
        DeserializationFailure = deserializationFailure;
    }

    public Guid DurableOperationId { get; }

    public DurableOperationStatus? Status { get; }

    public DateTimeOffset? UpdatedAtUtc { get; }

    public TResult? Result { get; }

    public bool HasResult { get; }

    public string? FailureReason { get; }

    public DurableOperationResultDeserializationFailure? DeserializationFailure { get; }

    public DurableOperationResultState State => DeserializationFailure is not null
        ? DurableOperationResultState.ResultDeserializationFailed
        : Status switch
        {
            null => DurableOperationResultState.Unknown,
            DurableOperationStatus.Pending => DurableOperationResultState.Pending,
            DurableOperationStatus.Running => DurableOperationResultState.Running,
            DurableOperationStatus.Completed when HasResult => DurableOperationResultState.CompletedWithResult,
            DurableOperationStatus.Completed => DurableOperationResultState.CompletedWithoutResult,
            DurableOperationStatus.Failed => DurableOperationResultState.Failed,
            DurableOperationStatus.Cancelled => DurableOperationResultState.Cancelled,
            _ => throw new InvalidOperationException("Operation status is not supported."),
        };

    public bool IsKnown => Status is not null;

    public bool IsCompleted => Status == DurableOperationStatus.Completed;

    public bool IsTerminal => Status is DurableOperationStatus.Completed
        or DurableOperationStatus.Failed
        or DurableOperationStatus.Cancelled;
}
