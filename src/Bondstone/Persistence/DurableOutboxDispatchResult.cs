namespace Bondstone.Persistence;

public sealed record DurableOutboxDispatchResult
{
    public DurableOutboxDispatchResult(
        int claimedCount,
        int dispatchedCount,
        int retryScheduledCount,
        int deadLetteredCount,
        int staleCount)
    {
        ValidateCount(claimedCount, nameof(claimedCount));
        ValidateCount(dispatchedCount, nameof(dispatchedCount));
        ValidateCount(retryScheduledCount, nameof(retryScheduledCount));
        ValidateCount(deadLetteredCount, nameof(deadLetteredCount));
        ValidateCount(staleCount, nameof(staleCount));

        ClaimedCount = claimedCount;
        DispatchedCount = dispatchedCount;
        RetryScheduledCount = retryScheduledCount;
        DeadLetteredCount = deadLetteredCount;
        StaleCount = staleCount;
    }

    public int ClaimedCount { get; }

    public int DispatchedCount { get; }

    public int RetryScheduledCount { get; }

    public int DeadLetteredCount { get; }

    public int StaleCount { get; }

    public int CompletedCount => DispatchedCount + RetryScheduledCount + DeadLetteredCount;

    private static void ValidateCount(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Dispatch result counts must not be negative.");
        }
    }
}
