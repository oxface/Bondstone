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
