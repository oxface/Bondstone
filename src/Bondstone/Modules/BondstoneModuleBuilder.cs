using Bondstone.Messaging;
using Bondstone.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

public sealed class BondstoneModuleBuilder
{
    internal BondstoneModuleBuilder(
        IServiceCollection services,
        string name,
        IMessageTypeRegistry messageTypeRegistry,
        ModuleCommandRouteRegistry commandRouteRegistry,
        ModuleEventSubscriberRegistry eventSubscriberRegistry,
        BondstoneModuleRegistry moduleRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageTypeRegistry);
        ArgumentNullException.ThrowIfNull(commandRouteRegistry);
        ArgumentNullException.ThrowIfNull(eventSubscriberRegistry);
        ArgumentNullException.ThrowIfNull(moduleRegistry);

        Services = services;
        Name = name.NormalizeRequired(nameof(name), "Module name");
        _moduleRegistry = moduleRegistry;
        _moduleRegistry.RegisterModule(Name);
        Commands = new BondstoneModuleCommandBuilder(
            services,
            Name,
            messageTypeRegistry,
            commandRouteRegistry);
        Events = new BondstoneModuleEventBuilder(
            services,
            Name,
            messageTypeRegistry,
            eventSubscriberRegistry);
    }

    private readonly BondstoneModuleRegistry _moduleRegistry;

    public IServiceCollection Services { get; }

    public string Name { get; }

    public BondstoneModuleCommandBuilder Commands { get; }

    public BondstoneModuleEventBuilder Events { get; }

    public BondstoneModuleBuilder UseDurableMessaging()
    {
        _moduleRegistry.EnableDurableMessaging(Name);
        return this;
    }

    public BondstoneModuleBuilder UsePersistence(
        string providerName,
        Type? contextType = null)
    {
        _moduleRegistry.EnablePersistence(Name, providerName, contextType);
        return this;
    }
}
