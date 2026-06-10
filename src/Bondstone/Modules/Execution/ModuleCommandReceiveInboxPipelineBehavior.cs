using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Modules;

internal sealed class ModuleCommandReceiveInboxPipelineBehavior<TCommand>(
    DurableModuleInboxHandlerExecutorResolver inboxHandlerExecutorResolver)
    : IModuleCommandSystemPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly DurableModuleInboxHandlerExecutorResolver _inboxHandlerExecutorResolver =
        inboxHandlerExecutorResolver ?? throw new ArgumentNullException(nameof(inboxHandlerExecutorResolver));

    public int Order => ModuleCommandSystemPipelineOrder.ReceiveInbox;

    public async ValueTask HandleAsync(
        TCommand command,
        ModuleCommandExecutionContext context,
        ModuleCommandPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        DurableInboxRecord? receiveInboxRecord = context.ReceiveInboxRecord;
        if (receiveInboxRecord is null)
        {
            await next(ct);
            return;
        }

        if (!StringComparer.Ordinal.Equals(receiveInboxRecord.Key.ModuleName, context.ModuleName))
        {
            throw new InvalidOperationException(
                $"Receive inbox record targets module '{receiveInboxRecord.Key.ModuleName}', but command execution is for module '{context.ModuleName}'.");
        }

        IDurableInboxHandlerExecutor inboxHandlerExecutor =
            _inboxHandlerExecutorResolver.Resolve(context.ModuleName);

        DurableInboxHandleResult result = await inboxHandlerExecutor.HandleOnceAsync(
            receiveInboxRecord,
            handlerCt => next(handlerCt),
            ct);

        context.SetReceiveInboxResult(result);
    }
}
