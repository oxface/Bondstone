using Bondstone.Messaging;
using Bondstone.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

public sealed class ModuleQueryRoute
{
    internal ModuleQueryRoute(
        string moduleName,
        Type queryType,
        Type handlerType,
        Type resultType,
        ModuleQueryRouteInvoker invoke)
    {
        ArgumentNullException.ThrowIfNull(queryType);
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(resultType);
        ArgumentNullException.ThrowIfNull(invoke);

        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        QueryType = queryType;
        HandlerType = handlerType;
        ResultType = resultType;
        _invoke = invoke;
    }

    private readonly ModuleQueryRouteInvoker _invoke;

    public string ModuleName { get; }

    public Type QueryType { get; }

    public Type HandlerType { get; }

    public Type ResultType { get; }

    internal ValueTask<object?> InvokeAsync(
        IServiceProvider serviceProvider,
        object query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(query);

        return _invoke(
            serviceProvider,
            query,
            ct);
    }

    internal static ModuleQueryRoute Create<TQuery, TResult, THandler>(
        string moduleName)
        where TQuery : IQuery<TResult>
        where THandler : class, IQueryHandler<TQuery, TResult>
    {
        return new ModuleQueryRoute(
            moduleName,
            typeof(TQuery),
            typeof(THandler),
            typeof(TResult),
            InvokeAsync<TQuery, TResult, THandler>);
    }

    private static async ValueTask<object?> InvokeAsync<TQuery, TResult, THandler>(
        IServiceProvider serviceProvider,
        object query,
        CancellationToken ct)
        where TQuery : IQuery<TResult>
        where THandler : class, IQueryHandler<TQuery, TResult>
    {
        if (query is not TQuery typedQuery)
        {
            throw new ArgumentException(
                $"Query route for '{typeof(TQuery).FullName}' cannot handle '{query.GetType().FullName}'.",
                nameof(query));
        }

        THandler queryHandler = serviceProvider.GetRequiredService<THandler>();
        return await queryHandler.HandleAsync(typedQuery, ct);
    }
}

internal delegate ValueTask<object?> ModuleQueryRouteInvoker(
    IServiceProvider serviceProvider,
    object query,
    CancellationToken ct);
