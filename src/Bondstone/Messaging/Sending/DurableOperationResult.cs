namespace Bondstone.Messaging;

/// <summary>
/// Represents the observed result state for a durable operation.
/// </summary>
/// <typeparam name="TResult">The expected result payload type.</typeparam>
/// <remarks>
/// This type reports the state stored for a durable operation. It is not a
/// broker delivery ledger and does not infer failure from outbox retry,
/// receive ambiguity, native dead-letter state, or caller wait timeout.
/// </remarks>
public sealed record DurableOperationResult<TResult>
{
    public DurableOperationResult(
        Guid durableOperationId,
        DurableOperationStatus? status,
        DateTimeOffset? updatedAtUtc,
        TResult? result = default,
        bool hasResult = false,
        string? failureReason = null,
        DurableOperationDiagnosticContext? diagnosticContext = null)
        : this(
            durableOperationId,
            status,
            updatedAtUtc,
            result,
            hasResult,
            failureReason,
            diagnosticContext,
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
        DurableOperationDiagnosticContext? diagnosticContext,
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
        DiagnosticContext = diagnosticContext;
        DeserializationFailure = deserializationFailure;
    }

    /// <summary>
    /// Gets the durable operation identifier.
    /// </summary>
    public Guid DurableOperationId { get; }

    /// <summary>
    /// Gets the stored operation status, or <see langword="null"/> when no
    /// operation state was found.
    /// </summary>
    public DurableOperationStatus? Status { get; }

    /// <summary>
    /// Gets the stored operation update timestamp, or <see langword="null"/>
    /// when no operation state was found.
    /// </summary>
    public DateTimeOffset? UpdatedAtUtc { get; }

    /// <summary>
    /// Gets the deserialized result payload when one was stored and could be
    /// read as <typeparamref name="TResult"/>.
    /// </summary>
    public TResult? Result { get; }

    /// <summary>
    /// Gets whether <see cref="Result"/> contains a successfully deserialized
    /// result payload.
    /// </summary>
    /// <remarks>
    /// A completed operation can have <see cref="HasResult"/> equal to
    /// <see langword="false"/> when no result payload was stored or when
    /// result deserialization failed.
    /// </remarks>
    public bool HasResult { get; }

    /// <summary>
    /// Gets the stored failure or cancellation reason when one is available.
    /// </summary>
    public string? FailureReason { get; }

    /// <summary>
    /// Gets optional diagnostic context captured with operation state.
    /// </summary>
    public DurableOperationDiagnosticContext? DiagnosticContext { get; }

    /// <summary>
    /// Gets result deserialization failure details when a completed operation
    /// payload could not be read as <typeparamref name="TResult"/>.
    /// </summary>
    public DurableOperationResultDeserializationFailure? DeserializationFailure { get; }

    /// <summary>
    /// Gets a caller-friendly classification for the observed operation result.
    /// </summary>
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

    /// <summary>
    /// Gets whether an operation state row was found.
    /// </summary>
    public bool IsKnown => Status is not null;

    /// <summary>
    /// Gets whether the stored operation status is <see cref="DurableOperationStatus.Completed"/>.
    /// </summary>
    public bool IsCompleted => Status == DurableOperationStatus.Completed;

    /// <summary>
    /// Gets whether the stored operation status is completed, failed, or
    /// cancelled.
    /// </summary>
    public bool IsTerminal => Status is DurableOperationStatus.Completed
        or DurableOperationStatus.Failed
        or DurableOperationStatus.Cancelled;
}
