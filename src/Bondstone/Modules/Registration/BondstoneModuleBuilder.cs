using Bondstone.Messaging;
using Bondstone.Configuration;
using Bondstone.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

public sealed class BondstoneModuleBuilder
{
    internal BondstoneModuleBuilder(
        IServiceCollection services,
        BondstoneOutboxBuilder outbox,
        string name,
        IMessageTypeRegistry messageTypeRegistry,
        ModuleCommandRouteRegistry commandRouteRegistry,
        ModulePublishedEventRegistry publishedEventRegistry,
        ModuleEventSubscriberRegistry eventSubscriberRegistry,
        BondstoneModuleRegistry moduleRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(messageTypeRegistry);
        ArgumentNullException.ThrowIfNull(commandRouteRegistry);
        ArgumentNullException.ThrowIfNull(publishedEventRegistry);
        ArgumentNullException.ThrowIfNull(eventSubscriberRegistry);
        ArgumentNullException.ThrowIfNull(moduleRegistry);

        Services = services;
        _outbox = outbox;
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
            publishedEventRegistry,
            eventSubscriberRegistry);
    }

    private readonly BondstoneModuleRegistry _moduleRegistry;
    private readonly BondstoneOutboxBuilder _outbox;

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

    public BondstoneModuleBuilder UseOutboxPersistenceProvider(string providerName)
    {
        _outbox.MarkPersistenceProvider(providerName);
        return this;
    }
}
