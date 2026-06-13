using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModuleOutboxDispatchAggregator(
    IServiceProvider serviceProvider,
    DurableModulePersistenceRegistrationRegistry persistenceRegistrationRegistry)
    : IDurableOutboxDispatcher
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly DurableModuleOutboxDispatcherRegistration[] _dispatcherRegistrations =
        persistenceRegistrationRegistry?.OutboxDispatcherRegistrations.ToArray()
        ?? throw new ArgumentNullException(nameof(persistenceRegistrationRegistry));

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

        if (_dispatcherRegistrations.Length == 0)
        {
            throw new InvalidOperationException("No module durable outbox dispatchers are registered.");
        }

        var claimedCount = 0;
        var dispatchedCount = 0;
        var retryScheduledCount = 0;
        var terminalFailedCount = 0;
        var staleCount = 0;

        foreach (DurableModuleOutboxDispatcherRegistration registration in _dispatcherRegistrations)
        {
            int remainingCount = maxCount - claimedCount;
            if (remainingCount <= 0)
            {
                break;
            }

            IDurableOutboxDispatcher dispatcher = registration.CreateDispatcher(_serviceProvider);
            DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
                normalizedClaimedBy,
                leaseDuration,
                remainingCount,
                ct);

            claimedCount += result.ClaimedCount;
            dispatchedCount += result.DispatchedCount;
            retryScheduledCount += result.RetryScheduledCount;
            terminalFailedCount += result.TerminalFailedCount;
            staleCount += result.StaleCount;
        }

        return new DurableOutboxDispatchResult(
            claimedCount,
            dispatchedCount,
            retryScheduledCount,
            terminalFailedCount,
            staleCount);
    }
}
