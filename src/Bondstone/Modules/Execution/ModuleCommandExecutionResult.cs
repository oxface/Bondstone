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

public sealed record ModuleCommandExecutionResult<TResult>
{
    public ModuleCommandExecutionResult(
        TResult result,
        DurableInboxHandleResult? receiveInboxResult = null)
    {
        Result = result;
        ReceiveInboxResult = receiveInboxResult;
    }

    public TResult Result { get; }

    public DurableInboxHandleResult? ReceiveInboxResult { get; }
}
