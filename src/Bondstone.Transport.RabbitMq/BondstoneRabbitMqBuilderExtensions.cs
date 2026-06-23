using Bondstone.Configuration;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Bondstone.Transport.RabbitMq;

public static class BondstoneRabbitMqBuilderExtensions
{
    public static BondstoneBuilder UseRabbitMqDispatcher(
        this BondstoneBuilder builder,
        Action<RabbitMqEnvelopeDispatcherOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Outbox.UseRabbitMqDispatcher(configure);
        return builder;
    }

    public static BondstoneOutboxBuilder UseRabbitMqDispatcher(
        this BondstoneOutboxBuilder outbox,
        Action<RabbitMqEnvelopeDispatcherOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(outbox);
        ArgumentNullException.ThrowIfNull(configure);

        outbox.Services.Configure(configure);
        outbox.Services.Replace(
            ServiceDescriptor.Singleton<IDurableEnvelopeDispatcher, RabbitMqEnvelopeDispatcher>());
        outbox.MarkTransport("RabbitMq");
        return outbox;
    }

    public static BondstoneBuilder UseRabbitMqReceiveWorker(
        this BondstoneBuilder builder,
        Action<RabbitMqReceiveWorkerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new RabbitMqReceiveWorkerOptions();
        configure(options);
        builder.Services.AddSingleton(options.ToRegistration());
        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, RabbitMqReceiveWorker>());
        return builder;
    }
}
