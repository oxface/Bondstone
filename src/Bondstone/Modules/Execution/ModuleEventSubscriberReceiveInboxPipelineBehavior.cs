using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Modules;

internal sealed class ModuleEventSubscriberReceiveInboxPipelineBehavior<TEvent>(
    DurableModuleInboxHandlerExecutorResolver inboxHandlerExecutorResolver)
    : IModuleEventSubscriberSystemPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly DurableModuleInboxHandlerExecutorResolver _inboxHandlerExecutorResolver =
        inboxHandlerExecutorResolver ?? throw new ArgumentNullException(nameof(inboxHandlerExecutorResolver));

    public int Order => ModuleEventSubscriberSystemPipelineOrder.ReceiveInbox;

    public async ValueTask HandleAsync(
        TEvent integrationEvent,
        ModuleEventSubscriberExecutionContext context,
        ModuleEventSubscriberPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
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
                $"Receive inbox record targets module '{receiveInboxRecord.Key.ModuleName}', but event subscriber execution is for module '{context.ModuleName}'.");
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
