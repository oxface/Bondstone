using Bondstone.Messaging;

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
        return await ExecuteCoreAsync(
            moduleName,
            command,
            receiveContext: null,
            ct);
    }

    public async ValueTask<ModuleCommandExecutionResult> ExecuteAsync<TCommand>(
        string moduleName,
        TCommand command,
        ModuleCommandReceiveContext receiveContext,
        CancellationToken ct = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(receiveContext);

        return await ExecuteCoreAsync(
            moduleName,
            command,
            receiveContext,
            ct);
    }

    public async ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        CancellationToken ct = default)
    {
        return await ExecuteCoreAsync(
            moduleName,
            command,
            receiveContext: null,
            ct);
    }

    public async ValueTask<ModuleCommandExecutionResult> ExecuteAsync(
        string moduleName,
        object command,
        ModuleCommandReceiveContext receiveContext,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(receiveContext);

        return await ExecuteCoreAsync(
            moduleName,
            command,
            receiveContext,
            ct);
    }

    private async ValueTask<ModuleCommandExecutionResult> ExecuteCoreAsync<TCommand>(
        string moduleName,
        TCommand command,
        ModuleCommandReceiveContext? receiveContext,
        CancellationToken ct)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        ModuleCommandRoute route = _routeRegistry.GetByCommandType(
            moduleName,
            typeof(TCommand));

        return await route.InvokeAsync(
            _serviceProvider,
            command,
            receiveContext,
            ct);
    }

    private async ValueTask<ModuleCommandExecutionResult> ExecuteCoreAsync(
        string moduleName,
        object command,
        ModuleCommandReceiveContext? receiveContext,
        CancellationToken ct)
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
            receiveContext,
            ct);
    }
}
