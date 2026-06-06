using Bondstone.Messaging;
using Bondstone.Persistence;

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
        DurableInboxRecord? receiveInboxRecord,
        CancellationToken ct = default)
        where TCommand : ICommand;

    ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        CancellationToken ct = default);

    ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        DurableInboxRecord? receiveInboxRecord,
        CancellationToken ct = default);
}
