using Bondstone.Diagnostics;
using Bondstone.Utility;

namespace Bondstone.Modules;

internal sealed class ModuleCommandRouteRegistry : IModuleCommandRouteRegistry
{
    private readonly Dictionary<RouteKey, ModuleCommandRoute> _routesByCommandType = [];
    private readonly Dictionary<RouteMessageKey, ModuleCommandRoute> _routesByMessageTypeName = [];
    private readonly object _sync = new();

    public IReadOnlyCollection<ModuleCommandRoute> Routes
    {
        get
        {
            lock (_sync)
            {
                return _routesByCommandType.Values.ToArray();
            }
        }
    }

    public ModuleCommandRoute GetByCommandType(
        string moduleName,
        Type commandType)
    {
        if (TryGetByCommandType(moduleName, commandType, out ModuleCommandRoute? route))
        {
            return route!;
        }

        throw new KeyNotFoundException(
            $"No command route is registered for module '{NormalizeModuleName(moduleName)}' and command type '{commandType.FullName}'.");
    }

    public ModuleCommandRoute GetByMessageTypeName(
        string moduleName,
        string messageTypeName)
    {
        if (TryGetByMessageTypeName(moduleName, messageTypeName, out ModuleCommandRoute? route))
        {
            return route!;
        }

        throw new KeyNotFoundException(
            $"No durable command route is registered for module '{NormalizeModuleName(moduleName)}' and message type '{NormalizeMessageTypeName(messageTypeName)}'.");
    }

    public bool TryGetByCommandType(
        string moduleName,
        Type commandType,
        out ModuleCommandRoute? route)
    {
        ArgumentNullException.ThrowIfNull(commandType);

        var key = new RouteKey(NormalizeModuleName(moduleName), commandType);

        lock (_sync)
        {
            return _routesByCommandType.TryGetValue(key, out route);
        }
    }

    public bool TryGetByMessageTypeName(
        string moduleName,
        string messageTypeName,
        out ModuleCommandRoute? route)
    {
        var key = new RouteMessageKey(
            NormalizeModuleName(moduleName),
            NormalizeMessageTypeName(messageTypeName));

        lock (_sync)
        {
            return _routesByMessageTypeName.TryGetValue(key, out route);
        }
    }

    internal ModuleCommandRoute Register(ModuleCommandRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);

        var commandKey = new RouteKey(route.ModuleName, route.CommandType);

        lock (_sync)
        {
            if (_routesByCommandType.TryGetValue(commandKey, out ModuleCommandRoute? existingRoute))
            {
                if (existingRoute.HandlerType != route.HandlerType
                    || existingRoute.HandlerIdentity != route.HandlerIdentity
                    || existingRoute.MessageTypeName != route.MessageTypeName)
                {
                    string durableIdentityDetail =
                        existingRoute.MessageTypeName is not null || route.MessageTypeName is not null
                            ? $" Existing durable command message identity: '{existingRoute.MessageTypeName ?? "(none)"}'; attempted durable command message identity: '{route.MessageTypeName ?? "(none)"}'."
                            : string.Empty;

                    throw new BondstoneSetupException(
                        BondstoneSetupCodes.DuplicateDurableRegistration,
                        $"Module '{route.ModuleName}' already has a command route for '{route.CommandType.FullName}'.{durableIdentityDetail}");
                }

                return existingRoute;
            }

            if (route.MessageTypeName is not null)
            {
                var messageKey = new RouteMessageKey(route.ModuleName, route.MessageTypeName);

                if (_routesByMessageTypeName.TryGetValue(messageKey, out ModuleCommandRoute? existingMessageRoute))
                {
                    throw new BondstoneSetupException(
                        BondstoneSetupCodes.DuplicateDurableRegistration,
                        $"Module '{route.ModuleName}' already has a durable command route for durable command message identity '{route.MessageTypeName}' handled by '{existingMessageRoute.HandlerType.FullName}'.");
                }

                _routesByMessageTypeName.Add(messageKey, route);
            }

            _routesByCommandType.Add(commandKey, route);
            return route;
        }
    }

    private static string NormalizeModuleName(string moduleName)
    {
        return moduleName.NormalizeRequired(nameof(moduleName), "Module name");
    }

    private static string NormalizeMessageTypeName(string messageTypeName)
    {
        return messageTypeName.NormalizeRequired(nameof(messageTypeName), "Message type name");
    }

    private sealed record RouteKey(
        string ModuleName,
        Type CommandType);

    private sealed record RouteMessageKey(
        string ModuleName,
        string MessageTypeName);
}
