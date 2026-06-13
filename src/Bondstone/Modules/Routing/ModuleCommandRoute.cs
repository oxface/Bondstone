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
        Type? resultType,
        Func<IServiceProvider, object, ModuleCommandRoute, ModuleCommandReceiveContext?, CancellationToken, ValueTask<ModuleCommandRouteExecutionResult>> invoke)
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
        ResultType = resultType;
        _invoke = invoke;
    }

    private readonly Func<IServiceProvider, object, ModuleCommandRoute, ModuleCommandReceiveContext?, CancellationToken, ValueTask<ModuleCommandRouteExecutionResult>> _invoke;

    public string ModuleName { get; }

    public Type CommandType { get; }

    public MessageTypeRegistration? MessageTypeRegistration { get; }

    public bool IsDurable => MessageTypeRegistration is not null;

    public string? MessageTypeName => MessageTypeRegistration?.MessageTypeName;

    public string? HandlerIdentity { get; }

    public Type HandlerType { get; }

    public Type? ResultType { get; }

    public bool HasResult => ResultType is not null;

    internal ValueTask<ModuleCommandRouteExecutionResult> InvokeAsync(
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
            resultType: null,
            InvokeAsync<TCommand, THandler>);
    }

    internal static ModuleCommandRoute Create<TCommand, TResult, THandler>(
        string moduleName,
        MessageTypeRegistration? messageTypeRegistration = null,
        string? handlerIdentity = null)
        where TCommand : ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        return new ModuleCommandRoute(
            moduleName,
            typeof(TCommand),
            messageTypeRegistration,
            handlerIdentity,
            typeof(THandler),
            typeof(TResult),
            InvokeAsync<TCommand, TResult, THandler>);
    }

    private static async ValueTask<ModuleCommandRouteExecutionResult> InvokeAsync<TCommand, THandler>(
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

        ModuleCommandPipelinePlan<TCommand> plan = serviceProvider
            .GetRequiredService<ModuleCommandPipelinePlanner>()
            .BuildPlan<TCommand>(
                serviceProvider,
                route);

        ModuleCommandPipeline pipeline = BuildPipeline(
            typedCommand,
            route,
            receiveContext,
            plan,
            handler);

        await pipeline.InvokeAsync(ct);
        return new ModuleCommandRouteExecutionResult(
            Result: null,
            pipeline.Context.ReceiveInboxResult);
    }

    private static async ValueTask<ModuleCommandRouteExecutionResult> InvokeAsync<TCommand, TResult, THandler>(
        IServiceProvider serviceProvider,
        object command,
        ModuleCommandRoute route,
        ModuleCommandReceiveContext? receiveContext,
        CancellationToken ct)
        where TCommand : ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        if (command is not TCommand typedCommand)
        {
            throw new ArgumentException(
                $"Command route for '{typeof(TCommand).FullName}' cannot handle '{command.GetType().FullName}'.",
                nameof(command));
        }

        TResult? result = default;
        bool hasResult = false;
        ModuleCommandExecutionContext? executionContext = null;
        ModuleCommandPipelineNext handler = async handlerCt =>
        {
            THandler commandHandler = serviceProvider.GetRequiredService<THandler>();
            result = await commandHandler.HandleAsync(typedCommand, handlerCt);
            hasResult = true;

            if (receiveContext?.DurableOperationId is not null)
            {
                DurableOperationResultPayloadSerializer payloadSerializer =
                    serviceProvider.GetRequiredService<DurableOperationResultPayloadSerializer>();
                executionContext!.SetDurableOperationResultPayload(
                    payloadSerializer.Serialize(result));
            }
        };

        ModuleCommandPipelinePlan<TCommand> plan = serviceProvider
            .GetRequiredService<ModuleCommandPipelinePlanner>()
            .BuildPlan<TCommand>(
                serviceProvider,
                route);

        ModuleCommandPipeline pipeline = BuildPipeline(
            typedCommand,
            route,
            receiveContext,
            plan,
            handler);

        executionContext = pipeline.Context;
        await pipeline.InvokeAsync(ct);
        if (!hasResult)
        {
            if (pipeline.Context.ReceiveInboxResult?.Status is DurableInboxHandleStatus.AlreadyProcessed
                or DurableInboxHandleStatus.AlreadyReceived)
            {
                return new ModuleCommandRouteExecutionResult(
                    Result: null,
                    pipeline.Context.ReceiveInboxResult);
            }

            throw new InvalidOperationException(
                $"Result command handler '{typeof(THandler).FullName}' did not produce a result.");
        }

        return new ModuleCommandRouteExecutionResult(
            result,
            pipeline.Context.ReceiveInboxResult);
    }

    private static ModuleCommandPipeline BuildPipeline<TCommand>(
        TCommand command,
        ModuleCommandRoute route,
        ModuleCommandReceiveContext? receiveContext,
        ModuleCommandPipelinePlan<TCommand> plan,
        ModuleCommandPipelineNext handler)
        where TCommand : ICommand
    {
        var context = new ModuleCommandExecutionContext(
            route,
            receiveContext);
        ModuleCommandPipelineNext next = handler;

        for (int index = plan.Steps.Count - 1; index >= 0; index--)
        {
            IModuleCommandPipelineBehavior<TCommand> behavior = plan.Steps[index].Behavior;
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

internal sealed record ModuleCommandRouteExecutionResult(
    object? Result,
    DurableInboxHandleResult? ReceiveInboxResult);
