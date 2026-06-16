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
    }
}
