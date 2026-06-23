using System.Reflection;
using System.Runtime.ExceptionServices;
using Bondstone.Messaging;
using Bondstone.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Modules;

public sealed class BondstoneModuleQueryBuilder
{
    internal BondstoneModuleQueryBuilder(
        IServiceCollection services,
        string moduleName,
        ModuleQueryRouteRegistry queryRouteRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(queryRouteRegistry);

        Services = services;
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _queryRouteRegistry = queryRouteRegistry;
    }

    private readonly ModuleQueryRouteRegistry _queryRouteRegistry;

    public IServiceCollection Services { get; }

    public string ModuleName { get; }

    public ModuleQueryRoute RegisterHandler<TQuery, TResult, THandler>()
        where TQuery : IQuery<TResult>
        where THandler : class, IQueryHandler<TQuery, TResult>
    {
        Services.TryAddScoped<THandler>();

        ModuleQueryRoute route = ModuleQueryRoute.Create<TQuery, TResult, THandler>(
            ModuleName);

        return _queryRouteRegistry.Register(route);
    }

    public IReadOnlyCollection<ModuleQueryRoute> RegisterFromAssemblyContaining<TMarker>()
    {
        return RegisterFromAssembly(typeof(TMarker).Assembly);
    }

    public IReadOnlyCollection<ModuleQueryRoute> RegisterFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false }
                && !type.ContainsGenericParameters)
            .SelectMany(RegisterHandlerType)
            .ToArray();
    }

    private IReadOnlyCollection<ModuleQueryRoute> RegisterHandlerType(Type handlerType)
    {
        Type[] handlerInterfaces = GetClosedGenericInterfaces(
            handlerType,
            typeof(IQueryHandler<,>));

        return handlerInterfaces
            .Select(handlerInterface => RegisterClosedHandlerType(handlerType, handlerInterface))
            .ToArray();
    }

    private ModuleQueryRoute RegisterClosedHandlerType(
        Type handlerType,
        Type handlerInterface)
    {
        Type[] genericArguments = handlerInterface.GetGenericArguments();
        Type queryType = genericArguments[0];
        Type resultType = genericArguments[1];
        Services.TryAddScoped(handlerType);

        MethodInfo method = typeof(BondstoneModuleQueryBuilder)
            .GetMethod(nameof(RegisterClosedHandlerTypeCore), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(queryType, resultType, handlerType);

        try
        {
            return (ModuleQueryRoute)method.Invoke(this, [])!;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private ModuleQueryRoute RegisterClosedHandlerTypeCore<TQuery, TResult, THandler>()
        where TQuery : IQuery<TResult>
        where THandler : class, IQueryHandler<TQuery, TResult>
    {
        ModuleQueryRoute route = ModuleQueryRoute.Create<TQuery, TResult, THandler>(
            ModuleName);

        return _queryRouteRegistry.Register(route);
    }

    private static Type[] GetClosedGenericInterfaces(
        Type implementationType,
        Type openGenericType)
    {
        return implementationType
            .GetInterfaces()
            .Where(type => type.IsGenericType
                && type.GetGenericTypeDefinition() == openGenericType)
            .Distinct()
            .ToArray();
    }
}
