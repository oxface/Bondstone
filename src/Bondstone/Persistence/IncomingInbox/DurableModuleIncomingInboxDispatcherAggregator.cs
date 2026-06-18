using Bondstone.Utility;
using System.ComponentModel;

namespace Bondstone.Persistence;

[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class DurableModuleIncomingInboxDispatcherAggregator(
    IServiceProvider serviceProvider,
    DurableModulePersistenceRegistrationRegistry registrationRegistry)
    : IDurableIncomingInboxDispatcher
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly DurableModuleIncomingInboxDispatcherRegistration[] _registrations =
        registrationRegistry?.IncomingInboxDispatcherRegistrations.ToArray()
        ?? throw new ArgumentNullException(nameof(registrationRegistry));

    public async ValueTask<DurableIncomingInboxProcessingResult> ProcessAsync(
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
                "Maximum processing count must be positive.");
        }

        if (_registrations.Length == 0)
        {
            throw new InvalidOperationException("No module durable incoming inbox dispatchers are registered.");
        }

        var claimedCount = 0;
        var processedCount = 0;
        var retryScheduledCount = 0;
        var terminalFailedCount = 0;
        var staleCount = 0;

        foreach (DurableModuleIncomingInboxDispatcherRegistration registration in _registrations)
        {
            int remainingCount = maxCount - claimedCount;
            if (remainingCount <= 0)
            {
                break;
            }

            IDurableIncomingInboxDispatcher dispatcher = registration.CreateDispatcher(_serviceProvider);
            DurableIncomingInboxProcessingResult result = await dispatcher.ProcessAsync(
                normalizedClaimedBy,
                leaseDuration,
                remainingCount,
                ct);

            claimedCount += result.ClaimedCount;
            processedCount += result.ProcessedCount;
            retryScheduledCount += result.RetryScheduledCount;
            terminalFailedCount += result.TerminalFailedCount;
            staleCount += result.StaleCount;
        }

        return new DurableIncomingInboxProcessingResult(
            claimedCount,
            processedCount,
            retryScheduledCount,
            terminalFailedCount,
            staleCount);
    }
}
