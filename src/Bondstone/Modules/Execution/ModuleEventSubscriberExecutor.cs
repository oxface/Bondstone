using Bondstone.Messaging;

namespace Bondstone.Modules;

internal sealed class ModuleEventSubscriberExecutor(
    IServiceProvider serviceProvider,
    IModuleEventSubscriberRegistry subscriberRegistry)
    : IModuleEventSubscriberExecutor
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly IModuleEventSubscriberRegistry _subscriberRegistry =
        subscriberRegistry ?? throw new ArgumentNullException(nameof(subscriberRegistry));

    public async ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity,
        object integrationEvent,
        CancellationToken ct = default)
    {
        return await ExecuteCoreAsync(
            moduleName,
            messageTypeName,
            subscriberIdentity,
            integrationEvent,
            receiveContext: null,
            ct);
    }

    public async ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity,
        object integrationEvent,
        ModuleEventSubscriberReceiveContext receiveContext,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(receiveContext);

        return await ExecuteCoreAsync(
            moduleName,
            messageTypeName,
            subscriberIdentity,
            integrationEvent,
            receiveContext,
            ct);
    }

    public async ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync<TEvent>(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity,
        TEvent integrationEvent,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        return await ExecuteCoreAsync(
            moduleName,
            messageTypeName,
            subscriberIdentity,
            integrationEvent,
            receiveContext: null,
            ct);
    }

    public async ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync<TEvent>(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity,
        TEvent integrationEvent,
        ModuleEventSubscriberReceiveContext receiveContext,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(receiveContext);

        return await ExecuteCoreAsync(
            moduleName,
            messageTypeName,
            subscriberIdentity,
            integrationEvent,
            receiveContext,
            ct);
    }

    private async ValueTask<ModuleEventSubscriberExecutionResult> ExecuteCoreAsync(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity,
        object integrationEvent,
        ModuleEventSubscriberReceiveContext? receiveContext,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ct.ThrowIfCancellationRequested();

        ValidateIntegrationEvent(integrationEvent);

        ModuleEventSubscriberRegistration subscriber =
            _subscriberRegistry.GetSubscriber(
                moduleName,
                messageTypeName,
                subscriberIdentity);

        ValidateSubscriberEventType(
            subscriber,
            integrationEvent);

        return await subscriber.ExecuteAsync(
            _serviceProvider,
            integrationEvent,
            receiveContext,
            ct);
    }

    private static void ValidateIntegrationEvent(object integrationEvent)
    {
        if (integrationEvent is IIntegrationEvent)
        {
            return;
        }

        throw new ArgumentException(
            $"Event type '{integrationEvent.GetType().FullName}' must implement {nameof(IIntegrationEvent)}.",
            nameof(integrationEvent));
    }

    private static void ValidateSubscriberEventType(
        ModuleEventSubscriberRegistration subscriber,
        object integrationEvent)
    {
        if (subscriber.EventType == integrationEvent.GetType())
        {
            return;
        }

        throw new ArgumentException(
            $"Event subscriber '{subscriber.SubscriberIdentity}' for message type '{subscriber.MessageTypeName}' expects event type '{subscriber.EventType.FullName}', not '{integrationEvent.GetType().FullName}'.",
            nameof(integrationEvent));
    }
}
