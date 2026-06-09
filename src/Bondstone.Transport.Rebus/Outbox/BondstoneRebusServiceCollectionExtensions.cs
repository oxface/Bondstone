using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Rebus.Bus;

namespace Bondstone.Transport.Rebus.Outbox;

public static class BondstoneRebusServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneRebusOutboxTransport(
        this IServiceCollection services,
        IReadOnlyDictionary<string, string> destinationAddressesByTargetModule,
        Func<string, string>? destinationAddressConvention = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddBondstoneRebusOutboxTransport(
            RebusCommandDestinationTopology.FromConfiguredDestinations(
                destinationAddressesByTargetModule,
                destinationAddressConvention),
            RebusEventTopicTopology.Empty,
            []);
    }

    internal static IServiceCollection AddBondstoneRebusOutboxTransport(
        this IServiceCollection services,
        RebusCommandDestinationTopology commandTopology,
        RebusEventTopicTopology eventTopicTopology,
        IReadOnlyCollection<RebusEventSubscriptionBinding> eventSubscriptionBindings)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(commandTopology);
        ArgumentNullException.ThrowIfNull(eventTopicTopology);
        ArgumentNullException.ThrowIfNull(eventSubscriptionBindings);

        services.AddSingleton<IRebusOutboxDestinationResolver>(
            new RebusModuleDestinationResolver(commandTopology));
        services.AddSingleton<IRebusOutboxEventTopicResolver>(
            new RebusEventTopicResolver(eventTopicTopology));
        services.AddSingleton<IRebusCommandTopologyDiagnostics>(
            new RebusCommandTopologyDiagnostics(commandTopology));
        services.AddSingleton<IRebusEventTopologyDiagnostics>(
            new RebusEventTopologyDiagnostics(
                eventTopicTopology,
                eventSubscriptionBindings));
        services.TryAddTransient<IDurableOutboxTransport, RebusDurableOutboxTransport>();

        return services;
    }
}
