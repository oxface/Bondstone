using Bondstone.Messaging;
using Bondstone.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

public sealed class ModuleCommandRoute
{
    internal ModuleCommandRoute(
        string moduleName,
        Type commandType,
        MessageTypeRegistration? messageTypeRegistration,
        string? handlerIdentity,
        Type handlerType,
        Func<IServiceProvider, object, ModuleCommandRoute, CancellationToken, ValueTask> invoke)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(invoke);

        if (!typeof(ICommand).IsAssignableFrom(commandType))
        {
            throw new ArgumentException(
                $"Command type '{commandType.FullName}' must implement {nameof(ICommand)}.",
                nameof(commandType));
        }

        if (messageTypeRegistration is not null)
        {
            if (messageTypeRegistration.Kind != MessageKind.Command)
            {
                throw new ArgumentException(
                    $"Message type '{messageTypeRegistration.MessageTypeName}' is registered as '{messageTypeRegistration.Kind}', not '{MessageKind.Command}'.",
                    nameof(messageTypeRegistration));
            }

            if (messageTypeRegistration.ClrType != commandType)
            {
                throw new ArgumentException(
                    $"Message type '{messageTypeRegistration.MessageTypeName}' is registered for '{messageTypeRegistration.ClrType.FullName}', not '{commandType.FullName}'.",
                    nameof(messageTypeRegistration));
            }

            handlerIdentity = handlerIdentity.NormalizeRequired(
                nameof(handlerIdentity),
                "Handler identity");
        }

        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        CommandType = commandType;
        MessageTypeRegistration = messageTypeRegistration;
        HandlerIdentity = handlerIdentity;
        HandlerType = handlerType;
        _invoke = invoke;
    }

    private readonly Func<IServiceProvider, object, ModuleCommandRoute, CancellationToken, ValueTask> _invoke;

    public string ModuleName { get; }

    public Type CommandType { get; }

    public MessageTypeRegistration? MessageTypeRegistration { get; }

    public bool IsDurable => MessageTypeRegistration is not null;

    public string? MessageTypeName => MessageTypeRegistration?.MessageTypeName;

    public string? HandlerIdentity { get; }

    public Type HandlerType { get; }

    internal ValueTask InvokeAsync(
        IServiceProvider serviceProvider,
        object command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(command);

        return _invoke(serviceProvider, command, this, ct);
    }

    internal static ModuleCommandRoute Create<TCommand, THandler>(
        string moduleName,
        MessageTypeRegistration? messageTypeRegistration = null,
        string? handlerIdentity = null)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        return new ModuleCommandRoute(
            moduleName,
            typeof(TCommand),
            messageTypeRegistration,
            handlerIdentity,
            typeof(THandler),
            InvokeAsync<TCommand, THandler>);
    }

    private static async ValueTask InvokeAsync<TCommand, THandler>(
        IServiceProvider serviceProvider,
        object command,
        ModuleCommandRoute route,
        CancellationToken ct)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        if (command is not TCommand typedCommand)
        {
            throw new ArgumentException(
                $"Command route for '{typeof(TCommand).FullName}' cannot handle '{command.GetType().FullName}'.",
                nameof(command));
        }

        ModuleCommandPipelineNext handler = async handlerCt =>
        {
            THandler commandHandler = serviceProvider.GetRequiredService<THandler>();
            await commandHandler.HandleAsync(typedCommand, handlerCt);
        };

        IReadOnlyList<IModuleCommandPipelineBehavior<TCommand>> behaviors = serviceProvider
            .GetServices<IModuleCommandPipelineBehavior<TCommand>>()
            .ToArray();

        ModuleCommandPipelineNext pipeline = BuildPipeline(
            typedCommand,
            route,
            behaviors,
            handler);

        await pipeline(ct);
    }

    private static ModuleCommandPipelineNext BuildPipeline<TCommand>(
        TCommand command,
        ModuleCommandRoute route,
        IReadOnlyList<IModuleCommandPipelineBehavior<TCommand>> behaviors,
        ModuleCommandPipelineNext handler)
        where TCommand : ICommand
    {
        var context = new ModuleCommandPipelineContext(route);
        ModuleCommandPipelineNext next = handler;

        for (int index = behaviors.Count - 1; index >= 0; index--)
        {
            IModuleCommandPipelineBehavior<TCommand> behavior = behaviors[index];
            ModuleCommandPipelineNext current = next;
            next = behaviorCt => behavior.HandleAsync(
                command,
                context,
                current,
                behaviorCt);
        }

        return next;
    }
}
