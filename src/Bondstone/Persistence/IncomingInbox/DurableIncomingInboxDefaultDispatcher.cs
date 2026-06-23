using Bondstone.Modules;

namespace Bondstone.Persistence;

internal sealed class DurableIncomingInboxDefaultDispatcher(
    IDurableIncomingInboxClaimer claimer,
    IModuleCommandReceivePipeline commandReceivePipeline,
    IModuleEventReceivePipeline eventReceivePipeline,
    IDurableIncomingInboxOutcomeRecorder outcomeRecorder,
    IDurableIncomingInboxFailurePolicy failurePolicy,
    TimeProvider? timeProvider = null)
    : IDurableIncomingInboxDispatcher
{
    private readonly DurableIncomingInboxDispatcher _dispatcher = new(
        claimer,
        commandReceivePipeline,
        eventReceivePipeline,
        outcomeRecorder,
        failurePolicy,
        timeProvider);

    public ValueTask<DurableIncomingInboxProcessingResult> ProcessAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken ct = default)
    {
        return _dispatcher.ProcessAsync(
            claimedBy,
            leaseDuration,
            maxCount,
            ct);
    }
}
