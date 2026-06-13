namespace Bondstone.Messaging;

public sealed record DurableOperationState
{
    public DurableOperationState(
        Guid durableOperationId,
        DurableOperationStatus status,
        DateTimeOffset updatedAtUtc,
        string? resultPayload = null,
        string? failureReason = null)
    {
        if (durableOperationId == Guid.Empty)
        {
            throw new ArgumentException("Durable operation id must not be empty.", nameof(durableOperationId));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Operation status is not supported.");
        }

        if (updatedAtUtc == default)
        {
            throw new ArgumentException("Updated timestamp must not be the default value.", nameof(updatedAtUtc));
        }

        if (updatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Updated timestamp must use UTC offset.", nameof(updatedAtUtc));
        }

        DurableOperationId = durableOperationId;
        Status = status;
        UpdatedAtUtc = updatedAtUtc;
        ResultPayload = string.IsNullOrWhiteSpace(resultPayload)
            ? null
            : resultPayload;
        FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? null
            : failureReason;
    }

    public Guid DurableOperationId { get; }

    public DurableOperationStatus Status { get; }

    public DateTimeOffset UpdatedAtUtc { get; }

    public string? ResultPayload { get; }

    public string? FailureReason { get; }
}
