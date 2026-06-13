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

        DurableOperationId = durableOperationId;
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
        Result = result;
        HasResult = hasResult;
        FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? null
            : failureReason;
    }

    public Guid DurableOperationId { get; }

    public DurableOperationStatus? Status { get; }

    public DateTimeOffset? UpdatedAtUtc { get; }

    public TResult? Result { get; }

    public bool HasResult { get; }

    public string? FailureReason { get; }

    public bool IsKnown => Status is not null;

    public bool IsCompleted => Status == DurableOperationStatus.Completed;

    public bool IsTerminal => Status is DurableOperationStatus.Completed
        or DurableOperationStatus.Failed
        or DurableOperationStatus.Cancelled;
}
