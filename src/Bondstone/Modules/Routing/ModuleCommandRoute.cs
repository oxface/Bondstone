using Bondstone.Messaging;
using Bondstone.Persistence;
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
        Func<IServiceProvider, object, ModuleCommandRoute, ModuleCommandReceiveContext?, CancellationToken, ValueTask<ModuleCommandExecutionResult>> invoke)
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

    private readonly Func<IServiceProvider, object, ModuleCommandRoute, ModuleCommandReceiveContext?, CancellationToken, ValueTask<ModuleCommandExecutionResult>> _invoke;

    public string ModuleName { get; }

    public Type CommandType { get; }

    public MessageTypeRegistration? MessageTypeRegistration { get; }

    public bool IsDurable => MessageTypeRegistration is not null;

    public string? MessageTypeName => MessageTypeRegistration?.MessageTypeName;

    public string? HandlerIdentity { get; }

    public Type HandlerType { get; }

    internal ValueTask<ModuleCommandExecutionResult> InvokeAsync(
        IServiceProvider serviceProvider,
        object command,
        ModuleCommandReceiveContext? receiveContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(command);

        return _invoke(
            serviceProvider,
            command,
            this,
            receiveContext,
            ct);
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

    private static async ValueTask<ModuleCommandExecutionResult> InvokeAsync<TCommand, THandler>(
        IServiceProvider serviceProvider,
        object command,
        ModuleCommandRoute route,
        ModuleCommandReceiveContext? receiveContext,
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

        IReadOnlyList<IModuleCommandPipelineBehavior<TCommand>> systemBehaviors = serviceProvider
            .GetServices<IModuleCommandSystemPipelineBehavior<TCommand>>()
            .OrderBy(static behavior => behavior.Order)
            .Cast<IModuleCommandPipelineBehavior<TCommand>>()
            .ToArray();
        IReadOnlyList<IModuleCommandPipelineBehavior<TCommand>> applicationBehaviors = serviceProvider
            .GetServices<IModuleCommandPipelineBehavior<TCommand>>()
            .ToArray();
        IReadOnlyList<IModuleCommandPipelineBehavior<TCommand>> behaviors = systemBehaviors
            .Concat(applicationBehaviors)
            .ToArray();

        ModuleCommandPipeline pipeline = BuildPipeline(
            typedCommand,
            route,
            receiveContext,
            behaviors,
            handler);

        await pipeline.InvokeAsync(ct);
        return new ModuleCommandExecutionResult(pipeline.Context.ReceiveInboxResult);
    }

    private static ModuleCommandPipeline BuildPipeline<TCommand>(
        TCommand command,
        ModuleCommandRoute route,
        ModuleCommandReceiveContext? receiveContext,
        IReadOnlyList<IModuleCommandPipelineBehavior<TCommand>> behaviors,
        ModuleCommandPipelineNext handler)
        where TCommand : ICommand
    {
        var context = new ModuleCommandExecutionContext(
            route,
            receiveContext);
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

        return new ModuleCommandPipeline(context, next);
    }

    private sealed record ModuleCommandPipeline(
        ModuleCommandExecutionContext Context,
        ModuleCommandPipelineNext Next)
    {
        public ValueTask InvokeAsync(CancellationToken ct)
        {
            return Next(ct);
        }
    }
}
