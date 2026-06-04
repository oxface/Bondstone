namespace Bondstone.Persistence;

public sealed record DurableOutboxDispatchState
{
    public DurableOutboxDispatchState(
        DurableOutboxStatus status,
        int attemptCount,
        DateTimeOffset? nextAttemptAtUtc = null,
        DateTimeOffset? dispatchedAtUtc = null,
        DateTimeOffset? failedAtUtc = null,
        string? failureReason = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Outbox status is not supported.");
        }

        if (attemptCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptCount),
                attemptCount,
                "Attempt count must not be negative.");
        }

        ValidateUtcTimestamp(nextAttemptAtUtc, nameof(nextAttemptAtUtc), "Next-attempt timestamp");
        ValidateUtcTimestamp(dispatchedAtUtc, nameof(dispatchedAtUtc), "Dispatched timestamp");
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");

        Status = status;
        AttemptCount = attemptCount;
        NextAttemptAtUtc = nextAttemptAtUtc;
        DispatchedAtUtc = dispatchedAtUtc;
        FailedAtUtc = failedAtUtc;
        FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? null
            : failureReason;
    }

    public static DurableOutboxDispatchState Pending { get; } = new(
        DurableOutboxStatus.Pending,
        attemptCount: 0);

    public DurableOutboxStatus Status { get; }

    public int AttemptCount { get; }

    public DateTimeOffset? NextAttemptAtUtc { get; }

    public DateTimeOffset? DispatchedAtUtc { get; }

    public DateTimeOffset? FailedAtUtc { get; }

    public string? FailureReason { get; }

    private static void ValidateUtcTimestamp(
        DateTimeOffset? value,
        string parameterName,
        string valueName)
    {
        if (value is null)
        {
            return;
        }

        if (value.Value == default)
        {
            throw new ArgumentException($"{valueName} must not be the default value.", parameterName);
        }

        if (value.Value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException($"{valueName} must use UTC offset.", parameterName);
        }
    }
}
