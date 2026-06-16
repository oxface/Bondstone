using Bondstone.Configuration;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bondstone.Transport.ServiceBus;

public static class BondstoneServiceBusBuilderExtensions
{
    public static BondstoneBuilder UseServiceBusDispatcher(
        this BondstoneBuilder builder,
        Action<ServiceBusEnvelopeDispatcherOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Outbox.UseServiceBusDispatcher(configure);
        return builder;
    }

    public static BondstoneOutboxBuilder UseServiceBusDispatcher(
        this BondstoneOutboxBuilder outbox,
        Action<ServiceBusEnvelopeDispatcherOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(configure);

        outbox.Services.Configure(configure);
        outbox.Services.Replace(
            ServiceDescriptor.Singleton<IDurableEnvelopeDispatcher, ServiceBusEnvelopeDispatcher>());
        outbox.MarkTransport("ServiceBus");
        return outbox;
    }

    public static BondstoneBuilder UseServiceBusReceiveWorker(
        this BondstoneBuilder builder,
        Action<ServiceBusReceiveWorkerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new ServiceBusReceiveWorkerOptions();
        configure(options);
        builder.Services.AddSingleton(options.ToRegistration());
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, ServiceBusReceiveWorker>());
        return builder;
    }
}
