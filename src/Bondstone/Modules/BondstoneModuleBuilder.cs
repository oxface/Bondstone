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
        BondstoneModuleRegistry moduleRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageTypeRegistry);
        ArgumentNullException.ThrowIfNull(commandRouteRegistry);
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
    }

    private readonly BondstoneModuleRegistry _moduleRegistry;

    public IServiceCollection Services { get; }

    public string Name { get; }

    public BondstoneModuleCommandBuilder Commands { get; }

    public BondstoneModuleBuilder UseDurableMessaging()
    {
        _moduleRegistry.EnableDurableMessaging(Name);
        return this;
    }
}
