using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableOutboxFailurePolicy : IDurableOutboxFailurePolicy
{
    public const int DefaultMaxAttempts = 5;

    public static IReadOnlyList<TimeSpan> DefaultRetryDelays { get; } =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(10),
        TimeSpan.FromMinutes(1),
        TimeSpan.FromMinutes(5),
        TimeSpan.FromMinutes(15),
    ];

    private readonly int _maxAttempts;
    private readonly TimeSpan[] _retryDelays;

    public DurableOutboxFailurePolicy(
        int maxAttempts = DefaultMaxAttempts,
        IReadOnlyList<TimeSpan>? retryDelays = null)
    {
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts),
                maxAttempts,
                "Max attempts must be greater than zero.");
        }

        TimeSpan[] normalizedRetryDelays = [.. retryDelays ?? DefaultRetryDelays];

        if (normalizedRetryDelays.Any(static delay => delay < TimeSpan.Zero))
        {
            throw new ArgumentException(
                "Retry delays must not contain negative durations.",
                nameof(retryDelays));
        }

        _maxAttempts = maxAttempts;
        _retryDelays = normalizedRetryDelays;
    }

    public DurableOutboxFailureDecision DecideFailure(
        DurableOutboxRecord record,
        string failureReason,
        DateTimeOffset failedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(record);
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");

        string normalizedFailureReason = failureReason.NormalizeRequired(
            nameof(failureReason),
            "Failure reason");
        DurableOutboxDispatchState dispatchState = record.DispatchState;

        if (dispatchState.Status != DurableOutboxStatus.Processing)
        {
            throw new ArgumentException(
                "Outbox failure decisions require a processing outbox record.",
                nameof(record));
        }

        if (dispatchState.AttemptCount <= 0)
        {
            throw new ArgumentException(
                "Processing outbox records must have a positive attempt count.",
                nameof(record));
        }

        if (dispatchState.AttemptCount >= _maxAttempts)
        {
            return DurableOutboxFailureDecision.TerminalFailure(
                normalizedFailureReason,
                failedAtUtc);
        }

        TimeSpan delay = GetRetryDelay(dispatchState.AttemptCount);
        return DurableOutboxFailureDecision.Retry(
            normalizedFailureReason,
            failedAtUtc,
            failedAtUtc.Add(delay));
    }

    private TimeSpan GetRetryDelay(int attemptCount)
    {
        if (_retryDelays.Length == 0)
        {
            return TimeSpan.Zero;
        }

        int delayIndex = Math.Min(attemptCount - 1, _retryDelays.Length - 1);
        return _retryDelays[delayIndex];
    }

    private static void ValidateUtcTimestamp(
        DateTimeOffset value,
        string parameterName,
        string valueName)
    {
        if (value == default)
        {
            throw new ArgumentException($"{valueName} must not be the default value.", parameterName);
        }

        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException($"{valueName} must use UTC offset.", parameterName);
        }
    }
}
