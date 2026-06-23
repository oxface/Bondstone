using Bondstone.Messaging;

namespace Bondstone.Modules;

internal sealed class ModuleQueryExecutor(
    IServiceProvider serviceProvider,
    IModuleQueryRouteRegistry routeRegistry,
    ModuleExecutionContextAccessor executionContextAccessor)
    : IModuleQueryExecutor
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IModuleQueryRouteRegistry _routeRegistry =
        routeRegistry ?? throw new ArgumentNullException(nameof(routeRegistry));
    private readonly ModuleExecutionContextAccessor _executionContextAccessor =
        executionContextAccessor ?? throw new ArgumentNullException(nameof(executionContextAccessor));

    public async ValueTask<TResult> ExecuteAsync<TResult>(
        string moduleName,
        IQuery<TResult> query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ModuleQueryRoute route = _routeRegistry.GetByQueryType(
            moduleName,
            query.GetType());

        if (route.ResultType != typeof(TResult))
        {
            throw new InvalidOperationException(
                $"Query route '{route.ModuleName}/{route.QueryType.FullName}' does not produce result type '{typeof(TResult).FullName}'.");
        }

        using IDisposable _ = _executionContextAccessor.PushNoContext();

        object? result = await route.InvokeAsync(
            _serviceProvider,
            query,
            ct);

        return (TResult)result!;
    }
}
