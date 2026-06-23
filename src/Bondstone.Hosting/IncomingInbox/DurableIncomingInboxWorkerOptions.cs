using Bondstone.Persistence;
using Bondstone.Utility;

namespace Bondstone.Hosting.IncomingInbox;

public sealed class DurableIncomingInboxWorkerOptions
{
    public string WorkerId { get; set; } = CreateDefaultWorkerId();

    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    public int BatchSize { get; set; } = 100;

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan FailureDelay { get; set; } = TimeSpan.FromSeconds(5);

    public int MaxAttempts { get; set; } = DurableIncomingInboxProcessingOptions.DefaultMaxAttempts;

    public IReadOnlyList<TimeSpan> RetryDelays { get; set; } =
        DurableIncomingInboxProcessingOptions.DefaultRetryDelays;

    public void Validate()
    {
        WorkerId = WorkerId.NormalizeRequired(nameof(WorkerId), "Worker id");
        ValidatePositive(LeaseDuration, nameof(LeaseDuration), "Lease duration");
        ValidatePositive(PollingInterval, nameof(PollingInterval), "Polling interval");
        ValidatePositive(FailureDelay, nameof(FailureDelay), "Failure delay");

        if (BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BatchSize),
                BatchSize,
                "Batch size must be positive.");
        }

        if (MaxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxAttempts),
                MaxAttempts,
                "Max attempts must be positive.");
        }

        if (RetryDelays is null)
        {
            throw new ArgumentNullException(nameof(RetryDelays));
        }

        if (RetryDelays.Any(static delay => delay < TimeSpan.Zero))
        {
            throw new ArgumentException(
                "Retry delays must not contain negative durations.",
                nameof(RetryDelays));
        }
    }

    internal DurableIncomingInboxProcessingOptions CreateProcessingOptions()
    {
        Validate();

        return new DurableIncomingInboxProcessingOptions(
            MaxAttempts,
            RetryDelays);
    }

    private static string CreateDefaultWorkerId()
    {
        return $"{Environment.MachineName}:{Environment.ProcessId}";
    }

    private static void ValidatePositive(
        TimeSpan value,
        string parameterName,
        string valueName)
    {
        if (value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"{valueName} must be positive.");
        }
    }
}
