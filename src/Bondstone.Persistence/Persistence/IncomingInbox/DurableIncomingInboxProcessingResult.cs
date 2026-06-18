namespace Bondstone.Persistence;

public sealed record DurableIncomingInboxProcessingResult
{
    public DurableIncomingInboxProcessingResult(
        int claimedCount,
        int processedCount,
        int retryScheduledCount,
        int terminalFailedCount,
        int staleCount)
    {
        ValidateCount(claimedCount, nameof(claimedCount));
        ValidateCount(processedCount, nameof(processedCount));
        ValidateCount(retryScheduledCount, nameof(retryScheduledCount));
        ValidateCount(terminalFailedCount, nameof(terminalFailedCount));
        ValidateCount(staleCount, nameof(staleCount));

        ClaimedCount = claimedCount;
        ProcessedCount = processedCount;
        RetryScheduledCount = retryScheduledCount;
        TerminalFailedCount = terminalFailedCount;
        StaleCount = staleCount;
    }

    public int ClaimedCount { get; }

    public int ProcessedCount { get; }

    public int RetryScheduledCount { get; }

    public int TerminalFailedCount { get; }

    public int StaleCount { get; }

    public int CompletedCount => ProcessedCount + RetryScheduledCount + TerminalFailedCount;

    private static void ValidateCount(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "Incoming inbox processing result counts must not be negative.");
        }
    }
}
