using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;

namespace Bondstone.Transport.RabbitMq.Outbox;

public static class BondstoneRabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneRabbitMqConnection(
        this IServiceCollection services,
        IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(connection);

        services.AddSingleton(connection);
        services.TryAddSingleton<IRabbitMqMessagePublisher, RabbitMqClientMessagePublisher>();

        return services;
    }

    internal static IServiceCollection AddBondstoneRabbitMqOutboxTransport(
        this IServiceCollection services,
        RabbitMqCommandRoutingTopology commandTopology,
        RabbitMqEventRoutingTopology eventTopology)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(commandTopology);
        ArgumentNullException.ThrowIfNull(eventTopology);

        services.AddSingleton(commandTopology);
        services.AddSingleton(eventTopology);
        services.AddTransient<IRabbitMqOutboxCommandRouteResolver>(
            serviceProvider => new RabbitMqCommandRouteResolver(
                serviceProvider.GetRequiredService<RabbitMqCommandRoutingTopology>()));
        services.AddTransient<IRabbitMqOutboxEventRouteResolver>(
            serviceProvider => new RabbitMqEventRouteResolver(
                serviceProvider.GetRequiredService<RabbitMqEventRoutingTopology>()));
        services.AddSingleton<IRabbitMqTopologyDiagnostics>(
            new RabbitMqTopologyDiagnostics(commandTopology, eventTopology));
        services.AddTransient<IDurableOutboxTransportRoute, RabbitMqDurableOutboxTransportRoute>();
        services.TryAddTransient<IDurableOutboxTransport, RoutedDurableOutboxTransport>();

        return services;
    }
}
