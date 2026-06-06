using Bondstone.Messaging;

namespace Bondstone.Modules;

public interface IModuleCommandExecutor
{
    ValueTask ExecuteAsync<TCommand>(
        string moduleName,
        TCommand command,
        CancellationToken ct = default)
        where TCommand : ICommand;

    ValueTask ExecuteAsync(
        string moduleName,
        object command,
        CancellationToken ct = default);
}
