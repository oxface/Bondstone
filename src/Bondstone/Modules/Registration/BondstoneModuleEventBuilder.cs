using Bondstone.Diagnostics;
using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Modules;

public sealed class BondstoneModuleEventBuilder
{
    internal BondstoneModuleEventBuilder(
        IServiceCollection services,
        string moduleName,
        IMessageTypeRegistry messageTypeRegistry,
        ModulePublishedEventRegistry publishedEventRegistry,
        ModuleEventSubscriberRegistry eventSubscriberRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageTypeRegistry);
        ArgumentNullException.ThrowIfNull(publishedEventRegistry);
        ArgumentNullException.ThrowIfNull(eventSubscriberRegistry);

        Services = services;
        ModuleName = moduleName;
        _messageTypeRegistry = messageTypeRegistry;
        _publishedEventRegistry = publishedEventRegistry;
        _eventSubscriberRegistry = eventSubscriberRegistry;
    }

    private readonly IMessageTypeRegistry _messageTypeRegistry;
    private readonly ModulePublishedEventRegistry _publishedEventRegistry;
    private readonly ModuleEventSubscriberRegistry _eventSubscriberRegistry;

    public IServiceCollection Services { get; }

    public string ModuleName { get; }

    public MessageTypeRegistration RegisterPublishedEvent<TEvent>()
        where TEvent : IIntegrationEvent
    {
        MessageTypeRegistration registration =
            RegisterMessageType<TEvent>("published event message identity");
        _publishedEventRegistry.Register(
            ModulePublishedEventRegistration.Create<TEvent>(
                ModuleName,
                registration));
        return registration;
    }

    public MessageTypeRegistration RegisterPublishedEvent<TEvent>(
        string messageTypeName)
        where TEvent : IIntegrationEvent
    {
        MessageTypeRegistration registration =
            RegisterMessageType<TEvent>(
                messageTypeName,
                "published event message identity");
        _publishedEventRegistry.Register(
            ModulePublishedEventRegistration.Create<TEvent>(
                ModuleName,
                registration));
        return registration;
    }

    public ModuleEventSubscriberRegistration RegisterSubscriber<TEvent, THandler>(
        string subscriberIdentity)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        MessageTypeRegistration registration =
            RegisterMessageType<TEvent>("event subscriber message identity");
        return RegisterSubscriber<TEvent, THandler>(
            registration,
            subscriberIdentity);
    }

    public ModuleEventSubscriberRegistration RegisterSubscriber<TEvent, THandler>(
        string messageTypeName,
        string subscriberIdentity)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        MessageTypeRegistration registration =
            RegisterMessageType<TEvent>(
                messageTypeName,
                "event subscriber message identity");
        return RegisterSubscriber<TEvent, THandler>(
            registration,
            subscriberIdentity);
    }

    private ModuleEventSubscriberRegistration RegisterSubscriber<TEvent, THandler>(
        MessageTypeRegistration registration,
        string subscriberIdentity)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        Services.TryAddScoped<THandler>();

        ModuleEventSubscriberRegistration subscriber =
            ModuleEventSubscriberRegistration.Create<TEvent, THandler>(
                ModuleName,
                registration,
                subscriberIdentity);

        return _eventSubscriberRegistry.Register(subscriber);
    }

    private MessageTypeRegistration RegisterMessageType<TEvent>(
        string registrationDescription)
        where TEvent : IIntegrationEvent
    {
        try
        {
            return _messageTypeRegistry.Register<TEvent>();
        }
        catch (InvalidOperationException exception)
        {
            throw CreateSetupException(
                exception,
                $"Module '{ModuleName}' could not register {registrationDescription} for event type '{typeof(TEvent).FullName}': {exception.Message}");
        }
    }

    private MessageTypeRegistration RegisterMessageType<TEvent>(
        string messageTypeName,
        string registrationDescription)
        where TEvent : IIntegrationEvent
    {
        try
        {
            return _messageTypeRegistry.Register<TEvent>(messageTypeName);
        }
        catch (InvalidOperationException exception)
        {
            throw CreateSetupException(
                exception,
                $"Module '{ModuleName}' could not register {registrationDescription} '{messageTypeName.Trim()}' for event type '{typeof(TEvent).FullName}': {exception.Message}");
        }
    }

    private static InvalidOperationException CreateSetupException(
        InvalidOperationException exception,
        string message)
    {
        return exception is IBondstoneSetupException setupException
            ? new BondstoneSetupException(setupException.SetupCode, message, exception)
            : new InvalidOperationException(message, exception);
    }
}
