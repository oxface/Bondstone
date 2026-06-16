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
        ModuleCommandRouteInvoker invoke)
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

    private readonly ModuleCommandRouteInvoker _invoke;

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

        Func<CancellationToken, ValueTask> handler = async handlerCt =>
        {
            THandler commandHandler = serviceProvider.GetRequiredService<THandler>();
            await commandHandler.HandleAsync(typedCommand, handlerCt);
        };

        var context = new ModuleCommandExecutionContext(
            route,
            receiveContext);

        IModuleCommandRuntime runtime = serviceProvider.GetRequiredService<IModuleCommandRuntime>();
        await runtime.ExecuteAsync(
            typedCommand,
            context,
            handler,
            ct);
        return new ModuleCommandRouteExecutionResult(
            Result: null,
            context.ReceiveInboxResult);
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
        var context = new ModuleCommandExecutionContext(
            route,
            receiveContext);
        Func<CancellationToken, ValueTask> handler = async handlerCt =>
        {
            THandler commandHandler = serviceProvider.GetRequiredService<THandler>();
            result = await commandHandler.HandleAsync(typedCommand, handlerCt);
            hasResult = true;

            if (receiveContext?.DurableOperationId is not null)
            {
                DurableOperationResultPayloadSerializer payloadSerializer =
                    serviceProvider.GetRequiredService<DurableOperationResultPayloadSerializer>();
                context.SetDurableOperationResultPayload(
                    payloadSerializer.Serialize(result));
            }
        };

        IModuleCommandRuntime runtime = serviceProvider.GetRequiredService<IModuleCommandRuntime>();
        await runtime.ExecuteAsync(
            typedCommand,
            context,
            handler,
            ct);
        if (!hasResult)
        {
            if (context.ReceiveInboxResult?.Status is DurableInboxHandleStatus.AlreadyProcessed
                or DurableInboxHandleStatus.AlreadyReceived)
            {
                return new ModuleCommandRouteExecutionResult(
                    Result: null,
                    context.ReceiveInboxResult);
            }

            throw new InvalidOperationException(
                $"Result command handler '{typeof(THandler).FullName}' did not produce a result.");
        }

        return new ModuleCommandRouteExecutionResult(
            result,
            context.ReceiveInboxResult);
    }
}

internal sealed record ModuleCommandRouteExecutionResult(
    object? Result,
    DurableInboxHandleResult? ReceiveInboxResult);

internal delegate ValueTask<ModuleCommandRouteExecutionResult> ModuleCommandRouteInvoker(
    IServiceProvider serviceProvider,
    object command,
    ModuleCommandRoute route,
    ModuleCommandReceiveContext? receiveContext,
    CancellationToken ct);
