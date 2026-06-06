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

    public async ValueTask ExecuteAsync<TCommand>(
        string moduleName,
        TCommand command,
        CancellationToken ct = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        ModuleCommandRoute route = _routeRegistry.GetByCommandType(
            moduleName,
            typeof(TCommand));

        await route.InvokeAsync(_serviceProvider, command, ct);
    }

    public async ValueTask ExecuteAsync(
        string moduleName,
        object command,
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

        await route.InvokeAsync(_serviceProvider, command, ct);
    }
}
