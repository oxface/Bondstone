using Bondstone.Configuration;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Outbox;

public sealed class BondstoneRebusServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneRebusOutboxTransport_WhenServicesIsNull_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(
            () => services!.AddBondstoneRebusOutboxTransport(new Dictionary<string, string>()));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneRebusOutboxTransport_RegistersTransportServices()
    {
        var services = new ServiceCollection();

        services.AddBondstoneRebusOutboxTransport(
            new Dictionary<string, string>
            {
                ["fulfillment"] = "fulfillment-queue",
            });

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IDurableOutboxTransport)
                && descriptor.ImplementationType == typeof(RebusDurableOutboxTransport));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusOutboxDestinationResolver)
                && descriptor.ImplementationInstance is RebusModuleDestinationResolver);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneRebusOutboxTransport_DoesNotRegisterDispatcher()
    {
        var services = new ServiceCollection();

        services.AddBondstoneRebusOutboxTransport(
            new Dictionary<string, string>
            {
                ["fulfillment"] = "fulfillment-queue",
            });

        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IDurableOutboxDispatcher));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusTransport_WhenUsedInBondstoneBuilder_RegistersTransportAndMarksCapability()
    {
        var services = new ServiceCollection();
        var transportWasMarked = false;

        services.AddBondstone(builder =>
        {
            builder.Outbox.UseRebusTransport(
                new Dictionary<string, string>
                {
                    ["fulfillment"] = "fulfillment-queue",
                });

            transportWasMarked = builder.Outbox.HasTransport;
        });

        Assert.True(transportWasMarked);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IDurableOutboxTransport)
                && descriptor.ImplementationType == typeof(RebusDurableOutboxTransport));
    }
}
