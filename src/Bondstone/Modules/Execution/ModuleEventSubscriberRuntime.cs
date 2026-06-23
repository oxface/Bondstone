using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Modules;

internal interface IModuleEventSubscriberRuntime
{
    ValueTask ExecuteAsync<TEvent>(
        TEvent integrationEvent,
        ModuleEventSubscriberExecutionContext context,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}

internal sealed class ModuleEventSubscriberRuntime(
    DurableModuleInboxHandlerExecutorResolver inboxHandlerExecutorResolver,
    ModuleExecutionContextAccessor executionContextAccessor,
    IEnumerable<IModuleTransactionRunner> transactionRunners,
    IEnumerable<IModulePostHandlerAction> postHandlerActions)
    : IModuleEventSubscriberRuntime
{
    private readonly DurableModuleInboxHandlerExecutorResolver _inboxHandlerExecutorResolver =
        inboxHandlerExecutorResolver ?? throw new ArgumentNullException(nameof(inboxHandlerExecutorResolver));
    private readonly ModuleExecutionContextAccessor _executionContextAccessor =
        executionContextAccessor ?? throw new ArgumentNullException(nameof(executionContextAccessor));
    private readonly IReadOnlyList<IModuleTransactionRunner> _transactionRunners =
        (transactionRunners ?? throw new ArgumentNullException(nameof(transactionRunners))).ToArray();
    private readonly IReadOnlyList<IModulePostHandlerAction> _postHandlerActions =
        (postHandlerActions ?? throw new ArgumentNullException(nameof(postHandlerActions))).ToArray();

    public ValueTask ExecuteAsync<TEvent>(
        TEvent integrationEvent,
        ModuleEventSubscriberExecutionContext context,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(handler);

        return ExecuteWithTransactionRunnersAsync(
            context,
            0,
            runnerCt => ExecuteInboxAsync(
                integrationEvent,
                context,
                handler,
                runnerCt),
            ct);
    }

    private ValueTask ExecuteWithTransactionRunnersAsync(
        IModuleRuntimeExecutionContext context,
        int index,
        Func<CancellationToken, ValueTask> operation,
        CancellationToken ct)
    {
        if (index >= _transactionRunners.Count)
        {
            return operation(ct);
        }

        IModuleTransactionRunner runner = _transactionRunners[index];
        return runner.ExecuteAsync(
            context,
            runnerCt => ExecuteWithTransactionRunnersAsync(
                context,
                index + 1,
                operation,
                runnerCt),
            ct);
    }

    private async ValueTask ExecuteInboxAsync<TEvent>(
        TEvent integrationEvent,
        ModuleEventSubscriberExecutionContext context,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct)
        where TEvent : IIntegrationEvent
    {
        DurableInboxRecord? receiveInboxRecord = context.ReceiveInboxRecord;
        if (receiveInboxRecord is null)
        {
            await ExecuteHandlerAsync(
                context,
                handler,
                ct);
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
            inboxCt => ExecuteHandlerAsync(
                context,
                handler,
                inboxCt),
            ct);

        context.SetReceiveInboxResult(result);
    }

    private async ValueTask ExecuteHandlerAsync(
        ModuleEventSubscriberExecutionContext context,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct)
    {
        using IDisposable scope = _executionContextAccessor.Push(
            new ModuleExecutionContext(context.ModuleName));

        await handler(ct);

        foreach (IModulePostHandlerAction action in _postHandlerActions)
        {
            await action.RunAsync(context, ct);
        }
    }
}
