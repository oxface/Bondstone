namespace Bondstone.Persistence;

public sealed class DurableIncomingInboxProcessingOptions
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

    public DurableIncomingInboxProcessingOptions(
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

        MaxAttempts = maxAttempts;
        RetryDelays = normalizedRetryDelays;
    }

    public int MaxAttempts { get; }

    public IReadOnlyList<TimeSpan> RetryDelays { get; }

}
