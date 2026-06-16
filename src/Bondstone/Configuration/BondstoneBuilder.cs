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
        ModulePublishedEventRegistry publishedEventRegistry,
        ModuleEventSubscriberRegistry eventSubscriberRegistry,
        BondstoneModuleRegistry moduleRegistry,
        ModulePipelineContributionRegistry pipelineContributionRegistry,
        ModuleCommandValidatorRegistry commandValidatorRegistry)
    {
        ArgumentNullException.ThrowIfNull(pipelineContributionRegistry);
        ArgumentNullException.ThrowIfNull(commandValidatorRegistry);

        Services = services;
        Outbox = new BondstoneOutboxBuilder(services);
        _messageTypeRegistry = messageTypeRegistry;
        _commandRouteRegistry = commandRouteRegistry;
        _publishedEventRegistry = publishedEventRegistry;
        _eventSubscriberRegistry = eventSubscriberRegistry;
        _moduleRegistry = moduleRegistry;
        _pipelineContributionRegistry = pipelineContributionRegistry;
        _commandValidatorRegistry = commandValidatorRegistry;
        _configurationValidators =
        [
            new BondstoneOutboxConfigurationValidator(Outbox),
            new DurableMessagingConfigurationValidator(),
        ];
    }

    private readonly IMessageTypeRegistry _messageTypeRegistry;
    private readonly ModuleCommandRouteRegistry _commandRouteRegistry;
    private readonly ModulePublishedEventRegistry _publishedEventRegistry;
    private readonly ModuleEventSubscriberRegistry _eventSubscriberRegistry;
    private readonly BondstoneModuleRegistry _moduleRegistry;
    private readonly ModulePipelineContributionRegistry _pipelineContributionRegistry;
    private readonly ModuleCommandValidatorRegistry _commandValidatorRegistry;
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
            _publishedEventRegistry,
            _eventSubscriberRegistry,
            _moduleRegistry,
            _pipelineContributionRegistry,
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
