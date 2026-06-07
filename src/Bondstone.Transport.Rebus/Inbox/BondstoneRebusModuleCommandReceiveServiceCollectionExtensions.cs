using Bondstone.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusModuleCommandReceiveServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneRebusModuleCommandReceiveTopology(
        this IServiceCollection services,
        IReadOnlyCollection<RebusModuleReceiveEndpointBinding> endpoints)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpoints);

        services.AddSingleton<IRebusModuleReceiveEndpointRegistry>(
            new RebusModuleReceiveEndpointRegistry(endpoints));
        services.AddBondstoneRebusModuleCommandReceivePipeline();

        return services;
    }

    public static IServiceCollection AddBondstoneRebusModuleCommandReceivePipeline(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<
            IRebusModuleCommandReceivePipeline,
            RebusModuleCommandReceivePipeline>();
        services.AddBondstoneDurablePayloadSerialization();

        return services;
    }
}
