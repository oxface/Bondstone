using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableModuleOutboxDispatchAggregator(
    IEnumerable<IDurableModuleOutboxDispatcher> moduleDispatchers)
    : IDurableOutboxDispatcher
{
    private readonly IDurableModuleOutboxDispatcher[] _moduleDispatchers =
        DurableModulePersistenceRegistrationValidator.ToValidatedArray(
            moduleDispatchers,
            static dispatcher => dispatcher.ModuleName,
            "durable module outbox dispatcher");

    public async ValueTask<DurableOutboxDispatchResult> DispatchAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default)
    {
        string normalizedClaimedBy = claimedBy.NormalizeRequired(nameof(claimedBy), "Claim owner");

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration),
                leaseDuration,
                "Lease duration must be positive.");
        }

        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCount),
                maxCount,
                "Maximum dispatch count must be positive.");
        }

        if (_moduleDispatchers.Length == 0)
        {
            throw new InvalidOperationException("No module durable outbox dispatchers are registered.");
        }

        var claimedCount = 0;
        var dispatchedCount = 0;
        var retryScheduledCount = 0;
        var deadLetteredCount = 0;
        var staleCount = 0;

        foreach (IDurableModuleOutboxDispatcher dispatcher in _moduleDispatchers)
        {
            int remainingCount = maxCount - claimedCount;
            if (remainingCount <= 0)
            {
                break;
            }

            DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
                normalizedClaimedBy,
                leaseDuration,
                remainingCount,
                ct);

            claimedCount += result.ClaimedCount;
            dispatchedCount += result.DispatchedCount;
            retryScheduledCount += result.RetryScheduledCount;
            deadLetteredCount += result.DeadLetteredCount;
            staleCount += result.StaleCount;
        }

        return new DurableOutboxDispatchResult(
            claimedCount,
            dispatchedCount,
            retryScheduledCount,
            deadLetteredCount,
            staleCount);
    }
}
