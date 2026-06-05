using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rebus.Bus;
using Rebus.Bus.Advanced;

namespace Bondstone.Transport.Rebus.Outbox;

public static class BondstoneRebusServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneRebusOutboxTransport(
        this IServiceCollection services,
        IReadOnlyDictionary<string, string> destinationAddressesByTargetModule)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<IRoutingApi>(
            static serviceProvider => serviceProvider.GetRequiredService<IBus>().Advanced.Routing);
        services.AddSingleton<IRebusOutboxDestinationResolver>(
            new RebusModuleDestinationResolver(destinationAddressesByTargetModule));
        services.TryAddTransient<IDurableOutboxTransport, RebusDurableOutboxTransport>();

        return services;
    }
}
