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
        services.TryAddSingleton<IRebusEventSubscriptionRegistry>(
            new RebusEventSubscriptionRegistry([]));
        services.AddBondstoneRebusDurableMessageReceiveServices();
        if (endpoints.Count == 1)
        {
            services.AddBondstoneRebusModuleCommandEndpointHandler(
                endpoints.Single().EndpointName);
        }

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

    public static IServiceCollection AddBondstoneRebusModuleEventReceiveTopology(
        this IServiceCollection services,
        IReadOnlyCollection<RebusEventSubscriptionBinding> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(subscriptions);

        services.TryAddSingleton<IRebusModuleReceiveEndpointRegistry>(
            new RebusModuleReceiveEndpointRegistry([]));
        services.AddSingleton<IRebusEventSubscriptionRegistry>(
            new RebusEventSubscriptionRegistry(subscriptions));
        services.AddBondstoneRebusDurableMessageReceiveServices();
        string[] endpointNames = subscriptions
            .Select(static subscription => subscription.EndpointName)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (endpointNames.Length == 1)
        {
            services.AddBondstoneRebusModuleCommandEndpointHandler(endpointNames.Single());
        }

        return services;
    }

    internal static IServiceCollection AddBondstoneRebusDurableMessageReceiveTopology(
        this IServiceCollection services,
        IReadOnlyCollection<RebusModuleReceiveEndpointBinding> endpoints,
        IReadOnlyCollection<RebusEventSubscriptionBinding> subscriptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(subscriptions);

        services.AddSingleton<IRebusModuleReceiveEndpointRegistry>(
            new RebusModuleReceiveEndpointRegistry(endpoints));
        services.AddSingleton<IRebusEventSubscriptionRegistry>(
            new RebusEventSubscriptionRegistry(subscriptions));
        services.AddBondstoneRebusDurableMessageReceiveServices();

        string[] endpointNames = endpoints
            .Select(static endpoint => endpoint.EndpointName)
            .Concat(subscriptions.Select(static subscription => subscription.EndpointName))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (endpointNames.Length == 1)
        {
            services.AddBondstoneRebusModuleCommandEndpointHandler(endpointNames.Single());
        }

        return services;
    }

    private static IServiceCollection AddBondstoneRebusDurableMessageReceiveServices(
        this IServiceCollection services)
    {
        services.AddBondstoneRebusModuleCommandReceivePipeline();
        services.TryAddTransient<
            IRebusModuleEventReceivePipeline,
            RebusModuleEventReceivePipeline>();
        services.TryAddTransient<
            IRebusModuleCommandEndpointDispatcher,
            RebusModuleCommandEndpointDispatcher>();
        services.TryAddTransient<
            IRebusModuleEventEndpointDispatcher,
            RebusModuleEventEndpointDispatcher>();
        services.TryAddTransient<
            IRebusDurableMessageEndpointDispatcher,
            RebusDurableMessageEndpointDispatcher>();
        services.AddBondstoneDurablePayloadSerialization();

        return services;
    }
}
