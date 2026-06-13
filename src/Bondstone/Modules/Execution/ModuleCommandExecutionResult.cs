using Bondstone.Persistence;

namespace Bondstone.Modules;

public sealed record ModuleCommandExecutionResult
{
    public ModuleCommandExecutionResult(DurableInboxHandleResult? receiveInboxResult = null)
    {
        ReceiveInboxResult = receiveInboxResult;
    }

    public DurableInboxHandleResult? ReceiveInboxResult { get; }
}

