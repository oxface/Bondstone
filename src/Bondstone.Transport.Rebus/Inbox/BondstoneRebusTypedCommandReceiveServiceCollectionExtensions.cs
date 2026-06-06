using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Transport.Rebus.Inbox;

public static class BondstoneRebusTypedCommandReceiveServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneRebusTypedCommandReceivePipeline(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBondstoneRebusInbox();
        services.TryAddTransient<
            IRebusTypedCommandReceivePipeline,
            RebusTypedCommandReceivePipeline>();

        return services;
    }
}
