using Bondstone.Persistence;

namespace Bondstone.Modules;

public sealed record ModuleEventSubscriberExecutionResult
{
    public ModuleEventSubscriberExecutionResult(
        DurableInboxHandleResult? receiveInboxResult = null)
    {
        ReceiveInboxResult = receiveInboxResult;
    }

    public DurableInboxHandleResult? ReceiveInboxResult { get; }
}
