using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusModuleCommandReceiveServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneRebusModuleCommandReceivePipeline(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<
            IRebusModuleCommandReceivePipeline,
            RebusModuleCommandReceivePipeline>();

        return services;
    }
}

