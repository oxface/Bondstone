using Microsoft.Extensions.DependencyInjection;
using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.Configuration;

public sealed class BondstoneBuilder
{
    internal BondstoneBuilder(
        IServiceCollection services,
        IMessageTypeRegistry messageTypeRegistry,
        ModuleCommandRouteRegistry commandRouteRegistry)
    {
        Services = services;
        Outbox = new BondstoneOutboxBuilder(services);
        _messageTypeRegistry = messageTypeRegistry;
        _commandRouteRegistry = commandRouteRegistry;
    }

    private readonly IMessageTypeRegistry _messageTypeRegistry;
    private readonly ModuleCommandRouteRegistry _commandRouteRegistry;

    public IServiceCollection Services { get; }

    public BondstoneOutboxBuilder Outbox { get; }

    internal BondstoneModuleBuilder CreateModuleBuilder(string moduleName)
    {
        return new BondstoneModuleBuilder(
            Services,
            moduleName,
            _messageTypeRegistry,
            _commandRouteRegistry);
    }

    internal void Validate()
    {
        Outbox.Validate();
    }
}
