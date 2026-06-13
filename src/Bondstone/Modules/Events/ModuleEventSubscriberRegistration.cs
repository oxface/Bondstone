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
        Func<IServiceProvider, object, ModuleEventSubscriberRegistration, ModuleEventSubscriberReceiveContext?, CancellationToken, ValueTask<ModuleEventSubscriberExecutionResult>> execute)
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

    private readonly Func<IServiceProvider, object, ModuleEventSubscriberRegistration, ModuleEventSubscriberReceiveContext?, CancellationToken, ValueTask<ModuleEventSubscriberExecutionResult>> _execute;

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

        ModuleEventSubscriberPipelineNext handler = CreateHandler<TEvent, THandler>(
            serviceProvider,
            typedEvent);
        ModuleEventSubscriberPipelinePlan<TEvent> plan = serviceProvider
            .GetRequiredService<ModuleEventSubscriberPipelinePlanner>()
            .BuildPlan<TEvent>(
                serviceProvider,
                subscriber);
        ModuleEventSubscriberPipeline pipeline = BuildPipeline(
            typedEvent,
            subscriber,
            receiveContext,
            plan,
            handler);

        await pipeline.InvokeAsync(ct);
        return new ModuleEventSubscriberExecutionResult(pipeline.Context.ReceiveInboxResult);
    }

    private static ModuleEventSubscriberPipelineNext CreateHandler<TEvent, THandler>(
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

    private static ModuleEventSubscriberPipeline BuildPipeline<TEvent>(
        TEvent integrationEvent,
        ModuleEventSubscriberRegistration subscriber,
        ModuleEventSubscriberReceiveContext? receiveContext,
        ModuleEventSubscriberPipelinePlan<TEvent> plan,
        ModuleEventSubscriberPipelineNext handler)
        where TEvent : IIntegrationEvent
    {
        var context = new ModuleEventSubscriberExecutionContext(
            subscriber,
            receiveContext);
        ModuleEventSubscriberPipelineNext next = handler;

        for (int index = plan.Steps.Count - 1; index >= 0; index--)
        {
            IModuleEventSubscriberPipelineBehavior<TEvent> behavior = plan.Steps[index].Behavior;
            ModuleEventSubscriberPipelineNext current = next;
            next = behaviorCt => behavior.HandleAsync(
                integrationEvent,
                context,
                current,
                behaviorCt);
        }

        return new ModuleEventSubscriberPipeline(context, next);
    }

    private sealed record ModuleEventSubscriberPipeline(
        ModuleEventSubscriberExecutionContext Context,
        ModuleEventSubscriberPipelineNext Next)
    {
        public ValueTask InvokeAsync(CancellationToken ct)
        {
            return Next(ct);
        }
    }
}
