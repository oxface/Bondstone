using System.Reflection;
using Bondstone.Messaging;
using Bondstone.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Modules;

public sealed class BondstoneModuleCommandBuilder
{
    internal BondstoneModuleCommandBuilder(
        IServiceCollection services,
        string moduleName,
        IMessageTypeRegistry messageTypeRegistry,
        ModuleCommandRouteRegistry commandRouteRegistry,
        ModuleCommandValidatorRegistry commandValidatorRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageTypeRegistry);
        ArgumentNullException.ThrowIfNull(commandRouteRegistry);
        ArgumentNullException.ThrowIfNull(commandValidatorRegistry);

        Services = services;
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        _messageTypeRegistry = messageTypeRegistry;
        _commandRouteRegistry = commandRouteRegistry;
        _commandValidatorRegistry = commandValidatorRegistry;
    }

    private readonly IMessageTypeRegistry _messageTypeRegistry;
    private readonly ModuleCommandRouteRegistry _commandRouteRegistry;
    private readonly ModuleCommandValidatorRegistry _commandValidatorRegistry;

    public IServiceCollection Services { get; }

    public string ModuleName { get; }

    public ModuleCommandRoute RegisterHandler<TCommand, THandler>(
        string? handlerIdentity = null)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        MessageTypeRegistration? registration = typeof(IDurableCommand).IsAssignableFrom(typeof(TCommand))
            ? _messageTypeRegistry.Register<TCommand>()
            : null;

        return RegisterHandler<TCommand, THandler>(
            registration,
            handlerIdentity ?? registration?.MessageTypeName);
    }

    public ModuleCommandRoute RegisterHandler<TCommand, THandler>(
        string messageTypeName,
        string? handlerIdentity = null)
        where TCommand : IDurableCommand
        where THandler : class, ICommandHandler<TCommand>
    {
        MessageTypeRegistration registration = _messageTypeRegistry.Register<TCommand>(messageTypeName);
        return RegisterHandler<TCommand, THandler>(
            registration,
            handlerIdentity ?? registration.MessageTypeName);
    }

    public BondstoneModuleCommandBuilder RegisterValidator<TCommand, TValidator>()
        where TCommand : ICommand
        where TValidator : class, ICommandValidator<TCommand>
    {
        Services.TryAddScoped<TValidator>();
        _commandValidatorRegistry.Add(new ModuleCommandValidatorRegistration(
            ModuleName,
            typeof(TCommand),
            typeof(TValidator)));
        return this;
    }

    public IReadOnlyCollection<ModuleCommandRoute> RegisterFromAssemblyContaining<TMarker>()
    {
        return RegisterFromAssembly(typeof(TMarker).Assembly);
    }

    public IReadOnlyCollection<ModuleCommandRoute> RegisterFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        Type[] concreteTypes = assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false })
            .ToArray();

        foreach (Type validatorType in concreteTypes)
        {
            RegisterValidatorType(validatorType);
        }

        return concreteTypes
            .SelectMany(RegisterHandlerType)
            .ToArray();
    }

    private ModuleCommandRoute RegisterHandler<TCommand, THandler>(
        MessageTypeRegistration? registration,
        string? handlerIdentity)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        Services.TryAddScoped<THandler>();

        ModuleCommandRoute route = ModuleCommandRoute.Create<TCommand, THandler>(
            ModuleName,
            registration,
            handlerIdentity);

        return _commandRouteRegistry.Register(route);
    }

    private IReadOnlyCollection<ModuleCommandRoute> RegisterHandlerType(Type handlerType)
    {
        Type[] handlerInterfaces = GetClosedGenericInterfaces(
            handlerType,
            typeof(ICommandHandler<>));

        return handlerInterfaces
            .Select(handlerInterface => RegisterClosedHandlerType(handlerType, handlerInterface))
            .ToArray();
    }

    private ModuleCommandRoute RegisterClosedHandlerType(
        Type handlerType,
        Type handlerInterface)
    {
        Type commandType = handlerInterface.GetGenericArguments()[0];
        MessageTypeRegistration? registration = typeof(IDurableCommand).IsAssignableFrom(commandType)
            ? _messageTypeRegistry.Register(commandType)
            : null;
        Services.TryAddScoped(handlerType);

        MethodInfo method = typeof(BondstoneModuleCommandBuilder)
            .GetMethod(nameof(RegisterClosedHandlerTypeCore), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(commandType, handlerType);

        return (ModuleCommandRoute)method.Invoke(this, [registration])!;
    }

    private ModuleCommandRoute RegisterClosedHandlerTypeCore<TCommand, THandler>(
        MessageTypeRegistration? registration)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        ModuleCommandRoute route = ModuleCommandRoute.Create<TCommand, THandler>(
            ModuleName,
            registration,
            registration?.MessageTypeName);

        return _commandRouteRegistry.Register(route);
    }

    private void RegisterValidatorType(Type validatorType)
    {
        foreach (Type validatorInterface in GetClosedGenericInterfaces(
            validatorType,
            typeof(ICommandValidator<>)))
        {
            Type commandType = validatorInterface.GetGenericArguments()[0];
            Services.TryAddScoped(validatorType);
            _commandValidatorRegistry.Add(new ModuleCommandValidatorRegistration(
                ModuleName,
                commandType,
                validatorType));
        }
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
