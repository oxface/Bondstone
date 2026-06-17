namespace Bondstone.Persistence;

public sealed record DurableIncomingInboxState
{
    public DurableIncomingInboxState(
        DurableIncomingInboxStatus status,
        int attemptCount,
        DateTimeOffset? nextAttemptAtUtc = null,
        DateTimeOffset? processedAtUtc = null,
        DateTimeOffset? failedAtUtc = null,
        string? failureReason = null,
        string? claimedBy = null,
        DateTimeOffset? claimedUntilUtc = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Durable incoming inbox status is not supported.");
        }

        if (attemptCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attemptCount),
                attemptCount,
                "Attempt count must not be negative.");
        }

        ValidateUtcTimestamp(nextAttemptAtUtc, nameof(nextAttemptAtUtc), "Next-attempt timestamp");
        ValidateUtcTimestamp(processedAtUtc, nameof(processedAtUtc), "Processed timestamp");
        ValidateUtcTimestamp(failedAtUtc, nameof(failedAtUtc), "Failed timestamp");
        ValidateUtcTimestamp(claimedUntilUtc, nameof(claimedUntilUtc), "Claim lease expiration timestamp");

        Status = status;
        AttemptCount = attemptCount;
        NextAttemptAtUtc = nextAttemptAtUtc;
        ProcessedAtUtc = processedAtUtc;
        FailedAtUtc = failedAtUtc;
        FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? null
            : failureReason;
        ClaimedBy = string.IsNullOrWhiteSpace(claimedBy)
            ? null
            : claimedBy.Trim();
        ClaimedUntilUtc = claimedUntilUtc;

        ValidateClaimPair();
        ValidateStatusShape();
    }

    public static DurableIncomingInboxState Pending { get; } = new(
        DurableIncomingInboxStatus.Pending,
        attemptCount: 0);

    public DurableIncomingInboxStatus Status { get; }

    public int AttemptCount { get; }

    public DateTimeOffset? NextAttemptAtUtc { get; }

    public DateTimeOffset? ProcessedAtUtc { get; }

    public DateTimeOffset? FailedAtUtc { get; }

    public string? FailureReason { get; }

    public string? ClaimedBy { get; }

    public DateTimeOffset? ClaimedUntilUtc { get; }

    private void ValidateClaimPair()
    {
        if (ClaimedBy is not null && ClaimedUntilUtc is null)
        {
            throw new ArgumentException(
                "Claim lease expiration is required when claim owner is provided.",
                "claimedUntilUtc");
        }

        if (ClaimedBy is null && ClaimedUntilUtc is not null)
        {
            throw new ArgumentException(
                "Claim owner is required when claim lease expiration is provided.",
                "claimedBy");
        }
    }

    private void ValidateStatusShape()
    {
        switch (Status)
        {
            case DurableIncomingInboxStatus.Pending:
                EnsureNoCompletedOrFailedOutcome();
                EnsureNoClaim();
                break;
            case DurableIncomingInboxStatus.Processing:
                EnsureNoCompletedOrFailedOutcome();
                RequireClaim();
                break;
            case DurableIncomingInboxStatus.Processed:
                RequireProcessedTimestamp();
                EnsureNoRetryOrFailureOutcome();
                EnsureNoClaim();
                break;
            case DurableIncomingInboxStatus.RetryScheduled:
                RequireFailureOutcome();
                RequireRetryTimestamp();
                EnsureNoClaim();
                break;
            case DurableIncomingInboxStatus.TerminalFailed:
                RequireFailureOutcome();
                EnsureNoRetryTimestamp();
                EnsureNoClaim();
                break;
        }
    }

    private void RequireClaim()
    {
        if (ClaimedBy is null)
        {
            throw new ArgumentException(
                "Claim owner is required for processing durable incoming inbox state.",
                "claimedBy");
        }

        if (ClaimedUntilUtc is null)
        {
            throw new ArgumentException(
                "Claim lease expiration is required for processing durable incoming inbox state.",
                "claimedUntilUtc");
        }
    }

    private void EnsureNoClaim()
    {
        if (ClaimedBy is not null)
        {
            throw new ArgumentException(
                "Claim owner is only valid for processing durable incoming inbox state.",
                "claimedBy");
        }

        if (ClaimedUntilUtc is not null)
        {
            throw new ArgumentException(
                "Claim lease expiration is only valid for processing durable incoming inbox state.",
                "claimedUntilUtc");
        }
    }

    private void EnsureNoCompletedOrFailedOutcome()
    {
        if (ProcessedAtUtc is not null)
        {
            throw new ArgumentException(
                "Processed timestamp is only valid for processed durable incoming inbox state.",
                "processedAtUtc");
        }

        EnsureNoRetryOrFailureOutcome();
    }

    private void EnsureNoRetryOrFailureOutcome()
    {
        if (NextAttemptAtUtc is not null)
        {
            throw new ArgumentException(
                "Next-attempt timestamp is only valid for retry-scheduled durable incoming inbox state.",
                "nextAttemptAtUtc");
        }

        if (FailedAtUtc is not null)
        {
            throw new ArgumentException(
                "Failed timestamp is only valid for retry-scheduled or terminal-failed durable incoming inbox state.",
                "failedAtUtc");
        }

        if (FailureReason is not null)
        {
            throw new ArgumentException(
                "Failure reason is only valid for retry-scheduled or terminal-failed durable incoming inbox state.",
                "failureReason");
        }
    }

    private void RequireProcessedTimestamp()
    {
        if (ProcessedAtUtc is null)
        {
            throw new ArgumentException(
                "Processed timestamp is required for processed durable incoming inbox state.",
                "processedAtUtc");
        }
    }

    private void RequireFailureOutcome()
    {
        if (ProcessedAtUtc is not null)
        {
            throw new ArgumentException(
                "Processed timestamp is not valid for retry-scheduled or terminal-failed durable incoming inbox state.",
                "processedAtUtc");
        }

        if (FailedAtUtc is null)
        {
            throw new ArgumentException(
                "Failed timestamp is required for retry-scheduled or terminal-failed durable incoming inbox state.",
                "failedAtUtc");
        }

        if (FailureReason is null)
        {
            throw new ArgumentException(
                "Failure reason is required for retry-scheduled or terminal-failed durable incoming inbox state.",
                "failureReason");
        }
    }

    private void RequireRetryTimestamp()
    {
        if (NextAttemptAtUtc is null)
        {
            throw new ArgumentException(
                "Next-attempt timestamp is required for retryable durable incoming inbox failure.",
                "nextAttemptAtUtc");
        }

        if (NextAttemptAtUtc < FailedAtUtc)
        {
            throw new ArgumentException(
                "Next-attempt timestamp must not be earlier than failed timestamp.",
                "nextAttemptAtUtc");
        }
    }

    private void EnsureNoRetryTimestamp()
    {
        if (NextAttemptAtUtc is not null)
        {
            throw new ArgumentException(
                "Terminal durable incoming inbox failures must not set a next-attempt timestamp.",
                "nextAttemptAtUtc");
        }
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
