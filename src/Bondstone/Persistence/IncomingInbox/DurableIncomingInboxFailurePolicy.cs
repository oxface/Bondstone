using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableIncomingInboxFailurePolicy(
    DurableIncomingInboxProcessingOptions? options = null)
    : IDurableIncomingInboxFailurePolicy
{
    private readonly DurableIncomingInboxProcessingOptions _options =
        options ?? new DurableIncomingInboxProcessingOptions();

    public DurableIncomingInboxFailureDecision DecideFailure(
        DurableIncomingInboxRecord record,
        string failureReason,
        DateTimeOffset failedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(record);
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");

        string normalizedFailureReason = failureReason.NormalizeRequired(
            nameof(failureReason),
            "Failure reason");
        DurableIncomingInboxState state = record.State;

        if (state.Status != DurableIncomingInboxStatus.Processing)
        {
            throw new ArgumentException(
                "Incoming inbox failure decisions require a processing incoming inbox record.",
                nameof(record));
        }

        if (state.AttemptCount <= 0)
        {
            throw new ArgumentException(
                "Processing incoming inbox records must have a positive attempt count.",
                nameof(record));
        }

        if (state.AttemptCount >= _options.MaxAttempts)
        {
            return DurableIncomingInboxFailureDecision.TerminalFailure(
                normalizedFailureReason,
                failedAtUtc);
        }

        TimeSpan delay = GetRetryDelay(state.AttemptCount);
        return DurableIncomingInboxFailureDecision.Retry(
            normalizedFailureReason,
            failedAtUtc,
            failedAtUtc.Add(delay));
    }

    private TimeSpan GetRetryDelay(int attemptCount)
    {
        if (_options.RetryDelays.Count == 0)
        {
            return TimeSpan.Zero;
        }

        int delayIndex = Math.Min(attemptCount - 1, _options.RetryDelays.Count - 1);
        return _options.RetryDelays[delayIndex];
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
