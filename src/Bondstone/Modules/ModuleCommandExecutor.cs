using Bondstone.Messaging;
using Bondstone.Persistence;

namespace Bondstone.Modules;

internal sealed class ModuleCommandExecutor(
    IServiceProvider serviceProvider,
    IModuleCommandRouteRegistry routeRegistry)
    : IModuleCommandExecutor
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IModuleCommandRouteRegistry _routeRegistry =
        routeRegistry ?? throw new ArgumentNullException(nameof(routeRegistry));

    public async ValueTask<ModuleCommandExecutionResult> ExecuteAsync<TCommand>(
        string moduleName,
        TCommand command,
        CancellationToken ct = default)
        where TCommand : ICommand
    {
        return await ExecuteAsync(
            moduleName,
            command,
            receiveInboxRecord: null,
            ct);
    }

    public async ValueTask<ModuleCommandExecutionResult> ExecuteAsync<TCommand>(
        string moduleName,
        TCommand command,
        DurableInboxRecord? receiveInboxRecord,
        CancellationToken ct = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        ModuleCommandRoute route = _routeRegistry.GetByCommandType(
            moduleName,
            typeof(TCommand));

        return await route.InvokeAsync(
            _serviceProvider,
            command,
            receiveInboxRecord,
            ct);
    }

    public async ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        CancellationToken ct = default)
    {
        return await ExecuteAsync(
            moduleName,
            command,
            receiveInboxRecord: null,
            ct);
    }

    public async ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        DurableInboxRecord? receiveInboxRecord,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command is not ICommand)
        {
            throw new ArgumentException(
                $"Command type '{command.GetType().FullName}' must implement {nameof(ICommand)}.",
                nameof(command));
        }

        ModuleCommandRoute route = _routeRegistry.GetByCommandType(
            moduleName,
            command.GetType());

        return await route.InvokeAsync(
            _serviceProvider,
            command,
            receiveInboxRecord,
            ct);
    }
}
