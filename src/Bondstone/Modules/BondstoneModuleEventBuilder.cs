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
        ModuleEventSubscriberRegistry eventSubscriberRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageTypeRegistry);
        ArgumentNullException.ThrowIfNull(eventSubscriberRegistry);

        Services = services;
        ModuleName = moduleName;
        _messageTypeRegistry = messageTypeRegistry;
        _eventSubscriberRegistry = eventSubscriberRegistry;
    }

    private readonly IMessageTypeRegistry _messageTypeRegistry;
    private readonly ModuleEventSubscriberRegistry _eventSubscriberRegistry;

    public IServiceCollection Services { get; }

    public string ModuleName { get; }

    public MessageTypeRegistration RegisterPublishedEvent<TEvent>()
        where TEvent : IIntegrationEvent
    {
        return _messageTypeRegistry.Register<TEvent>();
    }

    public MessageTypeRegistration RegisterPublishedEvent<TEvent>(
        string messageTypeName)
        where TEvent : IIntegrationEvent
    {
        return _messageTypeRegistry.Register<TEvent>(messageTypeName);
    }

    public ModuleEventSubscriberRegistration RegisterSubscriber<TEvent, THandler>(
        string subscriberIdentity)
        where TEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TEvent>
    {
        MessageTypeRegistration registration = _messageTypeRegistry.Register<TEvent>();
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
        MessageTypeRegistration registration = _messageTypeRegistry.Register<TEvent>(messageTypeName);
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
}
