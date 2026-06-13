using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Transport.Local.Outbox;

internal static class BondstoneLocalServiceCollectionExtensions
{
    internal static IServiceCollection AddBondstoneLocalOutboxTransport(
        this IServiceCollection services,
        LocalTransportTopology topology)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(topology);

        services.AddSingleton(topology);
        services.AddTransient<IDurableOutboxTransportRoute, LocalDurableOutboxTransportRoute>();
        services.TryAddTransient<IDurableOutboxTransport, RoutedDurableOutboxTransport>();

        return services;
    }
}
