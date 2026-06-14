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

    public async ValueTask<ModuleCommandExecutionResult<TResult>> ExecuteResultAsync<TResult>(
        string moduleName,
        ICommand<TResult> command,
        CancellationToken ct = default)
    {
        return await ExecuteResultCoreAsync(
            moduleName,
            command,
            receiveContext: null,
            ct);
    }

    public async ValueTask<ModuleCommandExecutionResult<TResult>> ExecuteResultAsync<TResult>(
        string moduleName,
        ICommand<TResult> command,
        ModuleCommandReceiveContext receiveContext,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(receiveContext);

        return await ExecuteResultCoreAsync(
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

        ModuleCommandRouteExecutionResult result = await route.InvokeAsync(
            _serviceProvider,
            command,
            receiveContext,
            ct);

        return new ModuleCommandExecutionResult(result.ReceiveInboxResult);
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

        ModuleCommandRouteExecutionResult result = await route.InvokeAsync(
            _serviceProvider,
            command,
            receiveContext,
            ct);

        return new ModuleCommandExecutionResult(result.ReceiveInboxResult);
    }

    private async ValueTask<ModuleCommandExecutionResult<TResult>> ExecuteResultCoreAsync<TResult>(
        string moduleName,
        ICommand<TResult> command,
        ModuleCommandReceiveContext? receiveContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        ModuleCommandRoute route = _routeRegistry.GetByCommandType(
            moduleName,
            command.GetType());

        if (route.ResultType != typeof(TResult))
        {
            throw new InvalidOperationException(
                $"Command route '{route.ModuleName}/{route.CommandType.FullName}' does not produce result type '{typeof(TResult).FullName}'.");
        }

        ModuleCommandRouteExecutionResult result = await route.InvokeAsync(
            _serviceProvider,
            command,
            receiveContext,
            ct);

        return new ModuleCommandExecutionResult<TResult>(
            (TResult)result.Result!,
            result.ReceiveInboxResult);
    }
}
