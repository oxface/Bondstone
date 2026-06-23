using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

internal interface IModuleCommandRuntime
{
    ValueTask ExecuteAsync<TCommand>(
        TCommand command,
        ModuleCommandExecutionContext context,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct = default)
        where TCommand : ICommand;
}

internal sealed class ModuleCommandRuntime(
    IServiceProvider serviceProvider,
    DurableModuleInboxHandlerExecutorResolver inboxHandlerExecutorResolver,
    DurableModuleOperationStateStoreResolver operationStateStoreResolver,
    ModuleCommandValidatorRegistry validatorRegistry,
    ModuleExecutionContextAccessor executionContextAccessor,
    IEnumerable<IModuleTransactionRunner> transactionRunners,
    IEnumerable<IModulePostHandlerAction> postHandlerActions)
    : IModuleCommandRuntime
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly DurableModuleInboxHandlerExecutorResolver _inboxHandlerExecutorResolver =
        inboxHandlerExecutorResolver ?? throw new ArgumentNullException(nameof(inboxHandlerExecutorResolver));
    private readonly DurableModuleOperationStateStoreResolver _operationStateStoreResolver =
        operationStateStoreResolver ?? throw new ArgumentNullException(nameof(operationStateStoreResolver));
    private readonly ModuleCommandValidatorRegistry _validatorRegistry =
        validatorRegistry ?? throw new ArgumentNullException(nameof(validatorRegistry));
    private readonly ModuleExecutionContextAccessor _executionContextAccessor =
        executionContextAccessor ?? throw new ArgumentNullException(nameof(executionContextAccessor));
    private readonly IReadOnlyList<IModuleTransactionRunner> _transactionRunners =
        (transactionRunners ?? throw new ArgumentNullException(nameof(transactionRunners))).ToArray();
    private readonly IReadOnlyList<IModulePostHandlerAction> _postHandlerActions =
        (postHandlerActions ?? throw new ArgumentNullException(nameof(postHandlerActions))).ToArray();

    public ValueTask ExecuteAsync<TCommand>(
        TCommand command,
        ModuleCommandExecutionContext context,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(handler);

        return ExecuteWithTransactionRunnersAsync(
            context,
            0,
            runnerCt => ExecuteOperationCompletionAsync(
                command,
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

    private async ValueTask ExecuteOperationCompletionAsync<TCommand>(
        TCommand command,
        ModuleCommandExecutionContext context,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct)
        where TCommand : ICommand
    {
        if (context.DurableOperationId is not Guid durableOperationId)
        {
            await ExecuteInboxAsync(
                command,
                context,
                handler,
                ct);
            return;
        }

        if (context.ReceiveInboxRecord is null)
        {
            throw new InvalidOperationException(
                $"Operation id '{durableOperationId}' was supplied without a receive inbox record. Operation completion is currently supported only for durable receive command execution.");
        }

        if (!context.Route.IsDurable)
        {
            throw new InvalidOperationException(
                $"Operation id '{durableOperationId}' was supplied for non-durable command route '{context.ModuleName}/{context.Route.CommandType.FullName}'.");
        }

        IDurableOperationStateStore operationStateStore =
            _operationStateStoreResolver.Resolve(
                context.ModuleName,
                durableOperationId);

        await ExecuteInboxAsync(
            command,
            context,
            handler,
            ct);

        if (context.ReceiveInboxResult is null)
        {
            throw new InvalidOperationException(
                $"Durable command execution with operation id '{durableOperationId}' did not produce a receive inbox result.");
        }

        if (context.ReceiveInboxResult.Status != DurableInboxHandleStatus.Handled)
        {
            return;
        }

        TimeProvider timeProvider =
            _serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

        await operationStateStore.SaveAsync(
            new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                timeProvider.GetUtcNow(),
                resultPayload: context.DurableOperationResultPayload,
                diagnosticContext: new DurableOperationDiagnosticContext(
                    context.ModuleName,
                    context.MessageTypeName,
                    context.HandlerIdentity)),
            ct);
    }

    private async ValueTask ExecuteInboxAsync<TCommand>(
        TCommand command,
        ModuleCommandExecutionContext context,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct)
        where TCommand : ICommand
    {
        DurableInboxRecord? receiveInboxRecord = context.ReceiveInboxRecord;
        if (receiveInboxRecord is null)
        {
            await ExecuteHandlerAsync(
                command,
                context,
                handler,
                ct);
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
            inboxCt => ExecuteHandlerAsync(
                command,
                context,
                handler,
                inboxCt),
            ct);

        context.SetReceiveInboxResult(result);
    }

    private async ValueTask ExecuteHandlerAsync<TCommand>(
        TCommand command,
        ModuleCommandExecutionContext context,
        Func<CancellationToken, ValueTask> handler,
        CancellationToken ct)
        where TCommand : ICommand
    {
        using IDisposable scope = _executionContextAccessor.Push(
            new ModuleExecutionContext(context.ModuleName));

        foreach (ModuleCommandValidatorRegistration registration in _validatorRegistry.GetValidators(
            context.ModuleName,
            typeof(TCommand)))
        {
            ICommandValidator<TCommand> validator = registration.CreateValidator<TCommand>(
                _serviceProvider);
            await validator.ValidateAsync(command, ct);
        }

        await handler(ct);

        foreach (IModulePostHandlerAction action in _postHandlerActions)
        {
            await action.RunAsync(context, ct);
        }
    }
}
