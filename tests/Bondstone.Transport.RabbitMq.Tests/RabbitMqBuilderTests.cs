using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bondstone.Transport.RabbitMq.Tests;

public sealed class RabbitMqBuilderTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void UseRabbitMqDispatcher_WhenConfigured_RegistersEnvelopeDispatcher()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Outbox.MarkPersistenceProvider("test persistence");
            bondstone.UseRabbitMqDispatcher(options =>
            {
                options.ResolveDestination = static _ =>
                    new RabbitMqEnvelopeDestination(
                        "bondstone.commands",
                        "fulfillment.commands");
            });
            bondstone.Outbox.MarkDispatcher("test dispatcher");
        });

        ServiceDescriptor descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IDurableEnvelopeDispatcher));
        Assert.Equal(
            "Bondstone.Transport.RabbitMq.RabbitMqEnvelopeDispatcher",
            descriptor.ImplementationType?.FullName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRabbitMqReceiveWorker_WhenQueueIsMissing_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
                bondstone.UseRabbitMqReceiveWorker(static options =>
                    options.ReceiveCommand())));

        Assert.Contains("QueueName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ReceiveWorkerOptions_DefaultsToNativeNackWithoutRequeue()
    {
        var options = new RabbitMqReceiveWorkerOptions();

        Assert.False(options.RequeueOnFailure);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRabbitMqReceiveWorker_WhenConfigured_RegistersHostedWorker()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.UseRabbitMqReceiveWorker(static options =>
            {
                options.QueueName = "fulfillment.commands";
                options.ReceiveCommand();
            });
        });

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IHostedService)
                && service.ImplementationType?.FullName
                    == "Bondstone.Transport.RabbitMq.RabbitMqReceiveWorker");
        RabbitMqReceiveWorkerRegistration registration = Assert.Single(
            services
                .Where(service => service.ServiceType == typeof(RabbitMqReceiveWorkerRegistration))
                .Select(service => service.ImplementationInstance)
                .Cast<RabbitMqReceiveWorkerRegistration>());
        Assert.Equal(RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion, registration.ReceiveMode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRabbitMqReceiveWorker_WhenDurableIncomingInboxCommandIngestionConfigured_RegistersIngestionMode()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.UseRabbitMqReceiveWorker(static options =>
            {
                options.QueueName = "fulfillment.commands";
                options.SourceTransportName = "rabbitmq:fulfillment-commands";
                options.IngestCommandToDurableIncomingInbox();
            });
        });

        RabbitMqReceiveWorkerRegistration registration = Assert.Single(
            services
                .Where(service => service.ServiceType == typeof(RabbitMqReceiveWorkerRegistration))
                .Select(service => service.ImplementationInstance)
                .Cast<RabbitMqReceiveWorkerRegistration>());
        Assert.Equal(RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion, registration.ReceiveMode);
        Assert.Null(registration.Binding);
        Assert.Equal("rabbitmq:fulfillment-commands", registration.SourceTransportName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRabbitMqReceiveWorker_WhenDurableIncomingInboxEventIngestionConfigured_RegistersSubscriberBinding()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.UseRabbitMqReceiveWorker(static options =>
            {
                options.QueueName = "billing.order-placed";
                options.IngestEventToDurableIncomingInbox(
                    "billing",
                    "billing.order-placed-projection.v1");
            });
        });

        RabbitMqReceiveWorkerRegistration registration = Assert.Single(
            services
                .Where(service => service.ServiceType == typeof(RabbitMqReceiveWorkerRegistration))
                .Select(service => service.ImplementationInstance)
                .Cast<RabbitMqReceiveWorkerRegistration>());
        Assert.Equal(RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion, registration.ReceiveMode);
        Assert.NotNull(registration.Binding);
        Assert.Equal("billing", registration.Binding.SubscriberModule);
        Assert.Equal("billing.order-placed-projection.v1", registration.Binding.SubscriberIdentity);
        Assert.Equal("rabbitmq:billing.order-placed", registration.SourceTransportName);
    }
}
