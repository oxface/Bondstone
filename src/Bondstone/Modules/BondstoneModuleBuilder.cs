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
        ModuleCommandRouteRegistry commandRouteRegistry)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(messageTypeRegistry);
        ArgumentNullException.ThrowIfNull(commandRouteRegistry);

        Services = services;
        Name = name.NormalizeRequired(nameof(name), "Module name");
        Commands = new BondstoneModuleCommandBuilder(
            services,
            Name,
            messageTypeRegistry,
            commandRouteRegistry);
    }

    public IServiceCollection Services { get; }

    public string Name { get; }

    public BondstoneModuleCommandBuilder Commands { get; }
}
