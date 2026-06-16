using Bondstone.Persistence;
using Bondstone.Transport.RabbitMq.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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

    internal static IServiceCollection AddBondstoneRabbitMqEnvelopeDispatcher(
        this IServiceCollection services,
        RabbitMqCommandRoutingTopology commandTopology,
        RabbitMqEventRoutingTopology eventTopology,
        RabbitMqReceiveTopology receiveTopology,
        RabbitMqReceiveWorkerRegistration? receiveWorkerRegistration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(commandTopology);
        ArgumentNullException.ThrowIfNull(eventTopology);
        ArgumentNullException.ThrowIfNull(receiveTopology);

        services.AddSingleton(commandTopology);
        services.AddSingleton(eventTopology);
        services.AddSingleton(receiveTopology);
        services.AddTransient<IRabbitMqOutboxCommandRouteResolver>(
            serviceProvider => new RabbitMqCommandRouteResolver(
                serviceProvider.GetRequiredService<RabbitMqCommandRoutingTopology>()));
        services.AddTransient<IRabbitMqOutboxEventRouteResolver>(
            serviceProvider => new RabbitMqEventRouteResolver(
                serviceProvider.GetRequiredService<RabbitMqEventRoutingTopology>()));
        services.AddSingleton<IRabbitMqTopologyDiagnostics>(
            new RabbitMqTopologyDiagnostics(commandTopology, eventTopology, receiveTopology));
        services.TryAddScoped<IRabbitMqReceivedMessageDispatcher, RabbitMqReceivedMessageDispatcher>();
        services.TryAddScoped<IRabbitMqReceivedMessageHandler, RabbitMqReceivedMessageHandler>();
        if (receiveWorkerRegistration is not null)
        {
            services.AddOptions<RabbitMqReceiveWorkerOptions>();
            if (receiveWorkerRegistration.ConfigureOptions is not null)
            {
                services.Configure(receiveWorkerRegistration.ConfigureOptions);
            }

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IHostedService, RabbitMqReceiveWorker>());
        }
        services.AddTransient<IDurableEnvelopeDispatchRoute, RabbitMqDurableEnvelopeDispatchRoute>();
        services.TryAddTransient<IDurableEnvelopeDispatcher, RoutedDurableEnvelopeDispatcher>();

        return services;
    }
}
