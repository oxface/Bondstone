using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.Configuration;

public sealed class BondstoneBuilder
{
    internal BondstoneBuilder(
        IServiceCollection services,
        IMessageTypeRegistry messageTypeRegistry,
        ModuleCommandRouteRegistry commandRouteRegistry,
        ModuleQueryRouteRegistry queryRouteRegistry,
        ModulePublishedEventRegistry publishedEventRegistry,
        ModuleEventSubscriberRegistry eventSubscriberRegistry,
        BondstoneModuleRegistry moduleRegistry,
        ModuleCommandValidatorRegistry commandValidatorRegistry)
    {
        ArgumentNullException.ThrowIfNull(commandValidatorRegistry);

        Services = services;
        Outbox = new BondstoneOutboxBuilder(services);
        _messageTypeRegistry = messageTypeRegistry;
        _commandRouteRegistry = commandRouteRegistry;
        _queryRouteRegistry = queryRouteRegistry;
        _publishedEventRegistry = publishedEventRegistry;
        _eventSubscriberRegistry = eventSubscriberRegistry;
        _moduleRegistry = moduleRegistry;
        _commandValidatorRegistry = commandValidatorRegistry;
        _configurationValidators =
        [
            new BondstoneOutboxConfigurationValidator(Outbox),
            new DurableMessagingConfigurationValidator(),
        ];
    }

    private readonly IMessageTypeRegistry _messageTypeRegistry;
    private readonly ModuleCommandRouteRegistry _commandRouteRegistry;
    private readonly ModuleQueryRouteRegistry _queryRouteRegistry;
    private readonly ModulePublishedEventRegistry _publishedEventRegistry;
    private readonly ModuleEventSubscriberRegistry _eventSubscriberRegistry;
    private readonly BondstoneModuleRegistry _moduleRegistry;
    private readonly ModuleCommandValidatorRegistry _commandValidatorRegistry;
    private readonly List<IBondstoneConfigurationValidator> _configurationValidators;

    public IServiceCollection Services { get; }

    public BondstoneOutboxBuilder Outbox { get; }

    /// <summary>
    /// Registers a durable message contract identity without registering a handler or subscriber.
    /// </summary>
    /// <typeparam name="TMessage">The durable command or integration event contract type.</typeparam>
    /// <returns>The registered message identity.</returns>
    public MessageTypeRegistration RegisterMessage<TMessage>()
        where TMessage : IMessage
    {
        return _messageTypeRegistry.Register<TMessage>();
    }

    /// <summary>
    /// Registers durable message contract identities from an assembly without registering handlers or subscribers.
    /// </summary>
    /// <param name="assembly">The assembly that contains attributed durable message contracts.</param>
    /// <returns>The registered message identities.</returns>
    public IReadOnlyCollection<MessageTypeRegistration> RegisterMessagesFromAssembly(
        Assembly assembly)
    {
        return _messageTypeRegistry.RegisterFromAssembly(assembly);
    }

    /// <summary>
    /// Registers durable message contract identities from the assembly that contains <typeparamref name="TMarker"/>.
    /// </summary>
    /// <typeparam name="TMarker">A marker type in the contract assembly.</typeparam>
    /// <returns>The registered message identities.</returns>
    public IReadOnlyCollection<MessageTypeRegistration> RegisterMessagesFromAssemblyContaining<TMarker>()
    {
        return RegisterMessagesFromAssembly(typeof(TMarker).Assembly);
    }

    internal BondstoneModuleBuilder CreateModuleBuilder(string moduleName)
    {
        return new BondstoneModuleBuilder(
            Services,
            Outbox,
            moduleName,
            _messageTypeRegistry,
            _commandRouteRegistry,
            _queryRouteRegistry,
            _publishedEventRegistry,
            _eventSubscriberRegistry,
            _moduleRegistry,
            _commandValidatorRegistry);
    }

    internal void Validate()
    {
        var context = new BondstoneConfigurationValidationContext(
            _moduleRegistry.Modules,
            _commandRouteRegistry.Routes,
            _publishedEventRegistry.PublishedEvents,
            _eventSubscriberRegistry.Subscribers,
            Outbox.TransportCount);
        foreach (IBondstoneConfigurationValidator validator in _configurationValidators)
        {
            validator.Validate(context);
        }
    }

    public BondstoneBuilder AddConfigurationValidator(
        IBondstoneConfigurationValidator validator)
    {
        ArgumentNullException.ThrowIfNull(validator);

        _configurationValidators.Add(validator);
        return this;
    }
}
