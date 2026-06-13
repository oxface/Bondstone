using Bondstone.Utility;

namespace Bondstone.Hosting.Outbox;

public sealed class DurableOutboxWorkerOptions
{
    public string WorkerId { get; set; } = CreateDefaultWorkerId();

    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);

    public int BatchSize { get; set; } = 100;

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    public TimeSpan FailureDelay { get; set; } = TimeSpan.FromSeconds(5);

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
