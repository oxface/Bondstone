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
        BondstoneModuleRegistry moduleRegistry)
    {
        Services = services;
        Outbox = new BondstoneOutboxBuilder(services);
        _messageTypeRegistry = messageTypeRegistry;
        _commandRouteRegistry = commandRouteRegistry;
        _moduleRegistry = moduleRegistry;
    }

    private readonly IMessageTypeRegistry _messageTypeRegistry;
    private readonly ModuleCommandRouteRegistry _commandRouteRegistry;
    private readonly BondstoneModuleRegistry _moduleRegistry;

    public IServiceCollection Services { get; }

    public BondstoneOutboxBuilder Outbox { get; }

    internal BondstoneModuleBuilder CreateModuleBuilder(string moduleName)
    {
        return new BondstoneModuleBuilder(
            Services,
            moduleName,
            _messageTypeRegistry,
            _commandRouteRegistry,
            _moduleRegistry);
    }

    internal void Validate()
    {
        Outbox.Validate();
    }
}
