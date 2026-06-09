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
        ServiceBusEventDestinationTopology eventDestinationTopology)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(commandTopology);
        ArgumentNullException.ThrowIfNull(eventDestinationTopology);

        services.AddSingleton(commandTopology);
        services.AddSingleton(eventDestinationTopology);
        services.AddTransient<IServiceBusOutboxDestinationResolver>(
            serviceProvider => new ServiceBusModuleQueueResolver(
                serviceProvider.GetRequiredService<ServiceBusCommandDestinationTopology>()));
        services.AddTransient<IServiceBusOutboxEventDestinationResolver>(
            serviceProvider => new ServiceBusEventDestinationResolver(
                serviceProvider.GetRequiredService<ServiceBusEventDestinationTopology>()));
        services.AddSingleton<IServiceBusTopologyDiagnostics>(
            new ServiceBusTopologyDiagnostics(commandTopology, eventDestinationTopology));
        services.AddTransient<IDurableOutboxTransportRoute, ServiceBusDurableOutboxTransportRoute>();
        services.TryAddTransient<IDurableOutboxTransport, RoutedDurableOutboxTransport>();

        return services;
    }
}
