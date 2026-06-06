using Bondstone.Configuration;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
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

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusTransport_WhenUsedWithTopologyBuilder_RegistersTransportAndMarksCapability()
    {
        var services = new ServiceCollection();
        var transportWasMarked = false;

        services.AddBondstone(builder =>
        {
            builder.UseRebusTransport(rebus =>
            {
                rebus.RouteModule("fulfillment").ToQueue("fulfillment-queue");
                rebus.RouteModule("billing").ToAddress("billing-commands");
            });

            transportWasMarked = builder.Outbox.HasTransport;
        });

        Assert.True(transportWasMarked);
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
    public void UseRebusTransport_WhenReceiveEndpointIsConfigured_RegistersReceiveTopologyAndPipeline()
    {
        var services = new ServiceCollection();

        services.AddBondstone(builder =>
        {
            builder.UseRebusTransport(rebus =>
            {
                rebus
                    .ReceiveEndpoint("fulfillment-commands")
                    .AcceptModule("fulfillment")
                    .ReceiveEndpoint("fulfillment-commands")
                    .AcceptModule("billing");
            });
        });

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusModuleReceiveEndpointRegistry)
                && descriptor.ImplementationInstance is RebusModuleReceiveEndpointRegistry);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusModuleCommandReceivePipeline)
                && descriptor.ImplementationType == typeof(RebusModuleCommandReceivePipeline));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusModuleReceiveEndpointRegistry registry =
            serviceProvider.GetRequiredService<IRebusModuleReceiveEndpointRegistry>();

        Assert.True(registry.EndpointAcceptsModule("fulfillment-commands", "fulfillment"));
        Assert.True(registry.EndpointAcceptsModule("fulfillment-commands", "billing"));
        Assert.True(registry.TryGetEndpointNameForModule("fulfillment", out string? endpointName));
        Assert.Equal("fulfillment-commands", endpointName);

        RebusModuleReceiveEndpointBinding endpoint =
            registry.GetEndpoint("fulfillment-commands");
        Assert.Equal(
            ["billing", "fulfillment"],
            endpoint.ModuleNames.OrderBy(static moduleName => moduleName, StringComparer.Ordinal));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusTransport_WhenConfigureIsNull_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(
            () => services.AddBondstone(builder => builder.UseRebusTransport(null!)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RouteModule_WhenTargetModuleIsBlank_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();

        Assert.Throws<ArgumentException>(() => builder.RouteModule(" "));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ToQueue_WhenQueueNameIsBlank_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();

        Assert.Throws<ArgumentException>(
            () => builder.RouteModule("fulfillment").ToQueue(" "));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RouteModule_WhenDuplicateDestinationMatches_AllowsIdempotentRegistration()
    {
        var builder = new BondstoneRebusTransportBuilder();

        builder.RouteModule("fulfillment").ToQueue("fulfillment-queue");
        builder.RouteModule("fulfillment").ToQueue("fulfillment-queue");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RouteModule_WhenDuplicateDestinationDiffers_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();
        builder.RouteModule("fulfillment").ToQueue("fulfillment-queue");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => builder.RouteModule("fulfillment").ToQueue("other-queue"));

        Assert.Contains("fulfillment", exception.Message);
        Assert.Contains("fulfillment-queue", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ReceiveEndpoint_WhenEndpointNameIsBlank_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();

        Assert.Throws<ArgumentException>(() => builder.ReceiveEndpoint(" "));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AcceptModule_WhenModuleNameIsBlank_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();

        Assert.Throws<ArgumentException>(
            () => builder.ReceiveEndpoint("fulfillment-commands").AcceptModule(" "));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AcceptModule_WhenDuplicateEndpointMatches_AllowsIdempotentRegistration()
    {
        var builder = new BondstoneRebusTransportBuilder();

        builder.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");
        builder.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AcceptModule_WhenModuleIsAcceptedByAnotherEndpoint_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();
        builder.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => builder.ReceiveEndpoint("priority-commands").AcceptModule("fulfillment"));

        Assert.Contains("fulfillment", exception.Message);
        Assert.Contains("fulfillment-commands", exception.Message);
    }
}
