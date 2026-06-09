using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed record DurableOutboxFailureDecision
{
    private DurableOutboxFailureDecision(
        DurableOutboxFailureDecisionKind kind,
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset? nextAttemptAtUtc = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Outbox failure decision kind is not supported.");
        }

        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");
        ValidateUtcTimestamp(nextAttemptAtUtc, nameof(nextAttemptAtUtc), "Next-attempt timestamp");

        if (kind == DurableOutboxFailureDecisionKind.Retry && nextAttemptAtUtc is null)
        {
            throw new ArgumentException(
                "Next-attempt timestamp is required for retry decisions.",
                nameof(nextAttemptAtUtc));
        }

        if (kind == DurableOutboxFailureDecisionKind.Retry && nextAttemptAtUtc < failedAtUtc)
        {
            throw new ArgumentException(
                "Next-attempt timestamp must not be earlier than failed timestamp.",
                nameof(nextAttemptAtUtc));
        }

        if (kind == DurableOutboxFailureDecisionKind.DeadLetter && nextAttemptAtUtc is not null)
        {
            throw new ArgumentException(
                "Dead-letter decisions must not set a next-attempt timestamp.",
                nameof(nextAttemptAtUtc));
        }

        Kind = kind;
        FailureReason = failureReason.NormalizeRequired(nameof(failureReason), "Failure reason");
        FailedAtUtc = failedAtUtc;
        NextAttemptAtUtc = nextAttemptAtUtc;
    }

    public DurableOutboxFailureDecisionKind Kind { get; }

    public string FailureReason { get; }

    public DateTimeOffset FailedAtUtc { get; }

    public DateTimeOffset? NextAttemptAtUtc { get; }

    public bool ShouldRetry => Kind == DurableOutboxFailureDecisionKind.Retry;

    public bool ShouldDeadLetter => Kind == DurableOutboxFailureDecisionKind.DeadLetter;

    public static DurableOutboxFailureDecision Retry(
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset nextAttemptAtUtc)
    {
        return new DurableOutboxFailureDecision(
            DurableOutboxFailureDecisionKind.Retry,
            failureReason,
            failedAtUtc,
            nextAttemptAtUtc);
    }

    public static DurableOutboxFailureDecision DeadLetter(
        string failureReason,
        DateTimeOffset failedAtUtc)
    {
        return new DurableOutboxFailureDecision(
            DurableOutboxFailureDecisionKind.DeadLetter,
            failureReason,
            failedAtUtc);
    }

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
