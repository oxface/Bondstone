using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Transport.Local.Outbox;

internal static class BondstoneLocalServiceCollectionExtensions
{
    internal static IServiceCollection AddBondstoneLocalEnvelopeDispatcher(
        this IServiceCollection services,
        LocalTransportTopology topology)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(topology);

        services.AddSingleton(topology);
        services.AddTransient<IDurableEnvelopeDispatchRoute, LocalDurableEnvelopeDispatchRoute>();
        services.TryAddTransient<IDurableEnvelopeDispatcher, RoutedDurableEnvelopeDispatcher>();

        return services;
    }
}
