using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed record DurableIncomingInboxFailureDecision
{
    private DurableIncomingInboxFailureDecision(
        DurableIncomingInboxFailureDecisionKind kind,
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset? nextAttemptAtUtc = null)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Durable incoming inbox failure decision kind is not supported.");
        }

        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");
        ValidateUtcTimestamp(nextAttemptAtUtc, nameof(nextAttemptAtUtc), "Next-attempt timestamp");

        if (kind == DurableIncomingInboxFailureDecisionKind.Retry && nextAttemptAtUtc is null)
        {
            throw new ArgumentException(
                "Next-attempt timestamp is required for retry decisions.",
                nameof(nextAttemptAtUtc));
        }

        if (kind == DurableIncomingInboxFailureDecisionKind.Retry && nextAttemptAtUtc < failedAtUtc)
        {
            throw new ArgumentException(
                "Next-attempt timestamp must not be earlier than failed timestamp.",
                nameof(nextAttemptAtUtc));
        }

        if (kind == DurableIncomingInboxFailureDecisionKind.TerminalFailure && nextAttemptAtUtc is not null)
        {
            throw new ArgumentException(
                "Terminal-failure decisions must not set a next-attempt timestamp.",
                nameof(nextAttemptAtUtc));
        }

        Kind = kind;
        FailureReason = failureReason.NormalizeRequired(nameof(failureReason), "Failure reason");
        FailedAtUtc = failedAtUtc;
        NextAttemptAtUtc = nextAttemptAtUtc;
    }

    public DurableIncomingInboxFailureDecisionKind Kind { get; }

    public string FailureReason { get; }

    public DateTimeOffset FailedAtUtc { get; }

    public DateTimeOffset? NextAttemptAtUtc { get; }

    public bool ShouldRetry => Kind == DurableIncomingInboxFailureDecisionKind.Retry;

    public bool ShouldTerminalFail => Kind == DurableIncomingInboxFailureDecisionKind.TerminalFailure;

    public static DurableIncomingInboxFailureDecision Retry(
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset nextAttemptAtUtc)
    {
        return new DurableIncomingInboxFailureDecision(
            DurableIncomingInboxFailureDecisionKind.Retry,
            failureReason,
            failedAtUtc,
            nextAttemptAtUtc);
    }

    public static DurableIncomingInboxFailureDecision TerminalFailure(
        string failureReason,
        DateTimeOffset failedAtUtc)
    {
        return new DurableIncomingInboxFailureDecision(
            DurableIncomingInboxFailureDecisionKind.TerminalFailure,
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
