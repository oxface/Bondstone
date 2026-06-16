using Bondstone.Messaging;
using Bondstone.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

public sealed class ModuleEventSubscriberRegistration
{
    internal ModuleEventSubscriberRegistration(
        string moduleName,
        Type eventType,
        MessageTypeRegistration messageTypeRegistration,
        string subscriberIdentity,
        Type handlerType,
        ModuleEventSubscriberInvoker execute)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(messageTypeRegistration);
        ArgumentNullException.ThrowIfNull(handlerType);
        ArgumentNullException.ThrowIfNull(execute);

        if (!typeof(IIntegrationEvent).IsAssignableFrom(eventType))
        {
            throw new ArgumentException(
                $"Event type '{eventType.FullName}' must implement {nameof(IIntegrationEvent)}.",
                nameof(eventType));
        }

        if (messageTypeRegistration.Kind != MessageKind.Event)
        {
            throw new ArgumentException(
                $"Message type '{messageTypeRegistration.MessageTypeName}' is registered as '{messageTypeRegistration.Kind}', not '{MessageKind.Event}'.",
                nameof(messageTypeRegistration));
        }

        if (messageTypeRegistration.ClrType != eventType)
        {
            throw new ArgumentException(
                $"Message type '{messageTypeRegistration.MessageTypeName}' is registered for '{messageTypeRegistration.ClrType.FullName}', not '{eventType.FullName}'.",
                nameof(messageTypeRegistration));
        }

        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        EventType = eventType;
        MessageTypeRegistration = messageTypeRegistration;
        SubscriberIdentity = subscriberIdentity.NormalizeRequired(
            nameof(subscriberIdentity),
            "Subscriber identity");
        HandlerType = handlerType;
        _execute = execute;
    }

    private readonly ModuleEventSubscriberInvoker _execute;

    public string ModuleName { get; }

    public Type EventType { get; }

    public MessageTypeRegistration MessageTypeRegistration { get; }

    public string MessageTypeName => MessageTypeRegistration.MessageTypeName;

    public string SubscriberIdentity { get; }

    public Type HandlerType { get; }

    internal ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync(
        IServiceProvider serviceProvider,
        object integrationEvent,
        ModuleEventSubscriberReceiveContext? receiveContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return _execute(
            serviceProvider,
            integrationEvent,
            this,
            receiveContext,
            ct);
    }

    internal static ModuleEventSubscriberRegistration Create<TEvent, THandler>(
        string moduleName,
        MessageTypeRegistration messageTypeRegistration,
        string subscriberIdentity)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        return new ModuleEventSubscriberRegistration(
            moduleName,
            typeof(TEvent),
            messageTypeRegistration,
            subscriberIdentity,
            typeof(THandler),
            ExecuteAsync<TEvent, THandler>);
    }

    private static async ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync<TEvent, THandler>(
        IServiceProvider serviceProvider,
        object integrationEvent,
        ModuleEventSubscriberRegistration subscriber,
        ModuleEventSubscriberReceiveContext? receiveContext,
        CancellationToken ct)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        if (integrationEvent is not TEvent typedEvent)
        {
            throw new ArgumentException(
                $"Event subscriber '{subscriber.SubscriberIdentity}' for '{typeof(TEvent).FullName}' cannot handle '{integrationEvent.GetType().FullName}'.",
                nameof(integrationEvent));
        }

        Func<CancellationToken, ValueTask> handler = CreateHandler<TEvent, THandler>(
            serviceProvider,
            typedEvent);
        var context = new ModuleEventSubscriberExecutionContext(
            subscriber,
            receiveContext);

        IModuleEventSubscriberRuntime runtime =
            serviceProvider.GetRequiredService<IModuleEventSubscriberRuntime>();
        await runtime.ExecuteAsync(
            typedEvent,
            context,
            handler,
            ct);
        return new ModuleEventSubscriberExecutionResult(context.ReceiveInboxResult);
    }

    private static Func<CancellationToken, ValueTask> CreateHandler<TEvent, THandler>(
        IServiceProvider serviceProvider,
        TEvent integrationEvent)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        return async handlerCt =>
        {
            THandler handler = serviceProvider.GetRequiredService<THandler>();
            await handler.HandleAsync(integrationEvent, handlerCt);
        };
    }
}

internal delegate ValueTask<ModuleEventSubscriberExecutionResult> ModuleEventSubscriberInvoker(
    IServiceProvider serviceProvider,
    object integrationEvent,
    ModuleEventSubscriberRegistration subscriber,
    ModuleEventSubscriberReceiveContext? receiveContext,
    CancellationToken ct);
