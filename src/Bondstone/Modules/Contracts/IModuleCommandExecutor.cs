using Bondstone.Messaging;

namespace Bondstone.Modules;

public interface IModuleCommandExecutor
{
    ValueTask<ModuleCommandExecutionResult> ExecuteAsync<TCommand>(
        string moduleName,
        TCommand command,
        CancellationToken ct = default)
        where TCommand : ICommand;

    ValueTask<ModuleCommandExecutionResult> ExecuteAsync<TCommand>(
        string moduleName,
        TCommand command,
        ModuleCommandReceiveContext receiveContext,
        CancellationToken ct = default)
        where TCommand : ICommand;

    ValueTask<ModuleCommandExecutionResult<TResult>> ExecuteResultAsync<TResult>(
        string moduleName,
        ICommand<TResult> command,
        CancellationToken ct = default);

    ValueTask<ModuleCommandExecutionResult<TResult>> ExecuteResultAsync<TResult>(
        string moduleName,
        ICommand<TResult> command,
        ModuleCommandReceiveContext receiveContext,
        CancellationToken ct = default);

    ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        CancellationToken ct = default);

    ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        ModuleCommandReceiveContext receiveContext,
        CancellationToken ct = default);
}
