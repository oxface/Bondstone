using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bondstone.Transport.ServiceBus.Tests;

public sealed class ServiceBusBuilderTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void UseServiceBusDispatcher_WhenConfigured_RegistersEnvelopeDispatcher()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Outbox.MarkPersistenceProvider("test persistence");
            bondstone.UseServiceBusDispatcher(options =>
            {
                options.ResolveEntityName = static _ => "fulfillment-commands";
            });
            bondstone.Outbox.MarkDispatcher("test dispatcher");
        });

        ServiceDescriptor descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IDurableEnvelopeDispatcher));
        Assert.Equal(
            "Bondstone.Transport.ServiceBus.ServiceBusEnvelopeDispatcher",
            descriptor.ImplementationType?.FullName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseServiceBusReceiveWorker_WhenEntityIsMissing_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
                bondstone.UseServiceBusReceiveWorker(static options =>
                    options.ReceiveCommand())));

        Assert.Contains("QueueName", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseServiceBusReceiveWorker_WhenConfigured_RegistersHostedWorker()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.UseServiceBusReceiveWorker(static options =>
            {
                options.TopicName = "integration-events";
                options.SubscriptionName = "fulfillment-order-placed";
                options.ReceiveEvent(
                    "fulfillment",
                    "fulfillment.order-placed-projection.v1");
            });
        });

        Assert.Contains(
            services,
            service => service.ServiceType == typeof(IHostedService)
                && service.ImplementationType?.FullName
                    == "Bondstone.Transport.ServiceBus.ServiceBusReceiveWorker");
    }
}
