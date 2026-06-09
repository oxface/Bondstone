using Azure.Messaging.ServiceBus;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Transport.ServiceBus.Outbox;

public static class BondstoneServiceBusServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneServiceBusClient(
        this IServiceCollection services,
        ServiceBusClient client)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(client);

        services.AddSingleton(client);
        services.TryAddSingleton<IServiceBusMessageSender, AzureServiceBusMessageSender>();

        return services;
    }

    internal static IServiceCollection AddBondstoneServiceBusOutboxTransport(
        this IServiceCollection services,
        ServiceBusCommandDestinationTopology commandTopology,
        ServiceBusEventTopicTopology eventTopicTopology)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(commandTopology);
        ArgumentNullException.ThrowIfNull(eventTopicTopology);

        services.AddSingleton<IServiceBusOutboxDestinationResolver>(
            new ServiceBusModuleQueueResolver(commandTopology));
        services.AddSingleton<IServiceBusOutboxEventTopicResolver>(
            new ServiceBusEventTopicResolver(eventTopicTopology));
        services.AddSingleton<IServiceBusTopologyDiagnostics>(
            new ServiceBusTopologyDiagnostics(commandTopology, eventTopicTopology));
        services.TryAddTransient<IDurableOutboxTransport, ServiceBusDurableOutboxTransport>();

        return services;
    }
}
