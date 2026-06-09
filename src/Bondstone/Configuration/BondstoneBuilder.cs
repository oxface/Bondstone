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
        ModuleEventSubscriberRegistry eventSubscriberRegistry,
        BondstoneModuleRegistry moduleRegistry)
    {
        Services = services;
        Outbox = new BondstoneOutboxBuilder(services);
        _messageTypeRegistry = messageTypeRegistry;
        _commandRouteRegistry = commandRouteRegistry;
        _eventSubscriberRegistry = eventSubscriberRegistry;
        _moduleRegistry = moduleRegistry;
        _configurationValidators =
        [
            new BondstoneOutboxConfigurationValidator(Outbox),
            new DurableMessagingConfigurationValidator(),
        ];
    }

    private readonly IMessageTypeRegistry _messageTypeRegistry;
    private readonly ModuleCommandRouteRegistry _commandRouteRegistry;
    private readonly ModuleEventSubscriberRegistry _eventSubscriberRegistry;
    private readonly BondstoneModuleRegistry _moduleRegistry;
    private readonly List<IBondstoneConfigurationValidator> _configurationValidators;

    public IServiceCollection Services { get; }

    public BondstoneOutboxBuilder Outbox { get; }

    internal BondstoneModuleBuilder CreateModuleBuilder(string moduleName)
    {
        return new BondstoneModuleBuilder(
            Services,
            Outbox,
            moduleName,
            _messageTypeRegistry,
            _commandRouteRegistry,
            _eventSubscriberRegistry,
            _moduleRegistry);
    }

    internal void Validate()
    {
        var context = new BondstoneConfigurationValidationContext(
            _moduleRegistry.Modules,
            _commandRouteRegistry.Routes,
            _eventSubscriberRegistry.Subscribers);
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
