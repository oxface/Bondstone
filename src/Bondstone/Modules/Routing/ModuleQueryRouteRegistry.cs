using Bondstone.Utility;

namespace Bondstone.Modules;

internal sealed class ModuleQueryRouteRegistry : IModuleQueryRouteRegistry
{
    private readonly Dictionary<RouteKey, ModuleQueryRoute> _routesByQueryType = [];
    private readonly object _sync = new();

    public IReadOnlyCollection<ModuleQueryRoute> Routes
    {
        get
        {
            lock (_sync)
            {
                return _routesByQueryType.Values.ToArray();
            }
        }
    }

    public ModuleQueryRoute GetByQueryType(
        string moduleName,
        Type queryType)
    {
        if (TryGetByQueryType(moduleName, queryType, out ModuleQueryRoute? route))
        {
            return route!;
        }

        throw new KeyNotFoundException(
            $"No query route is registered for module '{NormalizeModuleName(moduleName)}' and query type '{queryType.FullName}'.");
    }

    public bool TryGetByQueryType(
        string moduleName,
        Type queryType,
        out ModuleQueryRoute? route)
    {
        ArgumentNullException.ThrowIfNull(queryType);

        var key = new RouteKey(NormalizeModuleName(moduleName), queryType);

        lock (_sync)
        {
            return _routesByQueryType.TryGetValue(key, out route);
        }
    }

    internal ModuleQueryRoute Register(ModuleQueryRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var queryKey = new RouteKey(route.ModuleName, route.QueryType);

        lock (_sync)
        {
            if (_routesByQueryType.TryGetValue(queryKey, out ModuleQueryRoute? existingRoute))
            {
                if (existingRoute.HandlerType != route.HandlerType
                    || existingRoute.ResultType != route.ResultType)
                {
                    throw new InvalidOperationException(
                        $"Module '{route.ModuleName}' already has a query route for '{route.QueryType.FullName}'.");
                }

                return existingRoute;
            }

            _routesByQueryType.Add(queryKey, route);
            return route;
        }
    }

    private static string NormalizeModuleName(string moduleName)
    {
        return moduleName.NormalizeRequired(nameof(moduleName), "Module name");
    }

    private sealed record RouteKey(
        string ModuleName,
        Type QueryType);
}
