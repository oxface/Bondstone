using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Handlers;
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
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusOutboxEventTopicResolver)
                && descriptor.ImplementationInstance is RebusEventTopicResolver);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusCommandTopologyDiagnostics)
                && descriptor.ImplementationInstance is not null);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusEventTopologyDiagnostics)
                && descriptor.ImplementationInstance is not null);
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
            builder.Module("fulfillment", module =>
            {
                ConfigureDurableModule<ReserveOrderCommand, ReserveOrderHandler>(module);
            });
            builder.Module("billing", module =>
            {
                ConfigureDurableModule<CapturePaymentCommand, CapturePaymentHandler>(module);
            });
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
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IRebusModuleCommandEndpointDispatcher)
                && descriptor.ImplementationType?.Name == "RebusModuleCommandEndpointDispatcher");
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(RebusModuleCommandEndpointHandlerOptions)
                && descriptor.ImplementationInstance
                    is RebusModuleCommandEndpointHandlerOptions
                    {
                        EndpointName: "fulfillment-commands",
                    });
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHandleMessages<RebusDurableMessageEnvelope>)
                && descriptor.ImplementationType == typeof(RebusModuleCommandEndpointHandler));

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
    public void AddBondstoneRebusModuleCommandEndpointHandler_RegistersEndpointHandler()
    {
        var services = new ServiceCollection();

        services.AddBondstoneRebusModuleCommandEndpointHandler("fulfillment-commands");

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(RebusModuleCommandEndpointHandlerOptions)
                && descriptor.ImplementationInstance
                    is RebusModuleCommandEndpointHandlerOptions
                    {
                        EndpointName: "fulfillment-commands",
                    });
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(RebusModuleCommandEndpointHandler)
                && descriptor.ImplementationType == typeof(RebusModuleCommandEndpointHandler));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHandleMessages<RebusDurableMessageEnvelope>)
                && descriptor.ImplementationType == typeof(RebusModuleCommandEndpointHandler));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneRebusModuleCommandEndpointHandler_WhenReboundToDifferentEndpoint_Throws()
    {
        var services = new ServiceCollection();
        services.AddBondstoneRebusModuleCommandEndpointHandler("fulfillment-commands");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstoneRebusModuleCommandEndpointHandler("billing-commands"));

        Assert.Contains("fulfillment-commands", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusTransport_WhenReceiveModuleUsesConvention_RegistersDerivedReceiveTopology()
    {
        var services = new ServiceCollection();

        services.AddBondstone(builder =>
        {
            builder.Module("fulfillment", module =>
            {
                ConfigureDurableModule<ReserveOrderCommand, ReserveOrderHandler>(module);
            });
            builder.UseRebusTransport(rebus =>
            {
                rebus
                    .UseModuleQueueConvention(static moduleName => $"module-{moduleName}")
                    .ReceiveModule("fulfillment");
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusModuleReceiveEndpointRegistry registry =
            serviceProvider.GetRequiredService<IRebusModuleReceiveEndpointRegistry>();

        Assert.True(registry.EndpointAcceptsModule("module-fulfillment", "fulfillment"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusTransport_WhenReceiveEndpointTargetsMissingModule_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(builder =>
            {
                builder.UseRebusTransport(rebus =>
                {
                    rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");
                });
            }));

        Assert.Contains("fulfillment-commands", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not registered", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusTransport_WhenReceiveEndpointTargetsNonDurableModule_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(builder =>
            {
                builder.Module("fulfillment", _ => { });
                builder.UseRebusTransport(rebus =>
                {
                    rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");
                });
            }));

        Assert.Contains("fulfillment-commands", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("durable messaging", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseRebusTransport_WhenReceiveEndpointTargetsModuleWithNoDurableHandlers_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(builder =>
            {
                builder.Module("fulfillment", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                });
                builder.UseRebusTransport(rebus =>
                {
                    rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");
                });
            }));

        Assert.Contains("fulfillment-commands", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("durable command handlers", exception.Message, StringComparison.OrdinalIgnoreCase);
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
    public void UseModuleQueueConvention_WhenFactoryIsNull_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();

        Assert.Throws<ArgumentNullException>(
            () => builder.UseModuleQueueConvention(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ReceiveModule_WhenModuleNameIsBlank_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();

        Assert.Throws<ArgumentException>(() => builder.ReceiveModule(" "));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ReceiveModule_WhenConventionIsMissing_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => builder.ReceiveModule("fulfillment"));

        Assert.Contains(nameof(BondstoneRebusTransportBuilder.UseModuleQueueConvention), exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ReceiveModule_WhenConventionReturnsBlank_Throws()
    {
        var builder = new BondstoneRebusTransportBuilder();
        builder.UseModuleQueueConvention(static _ => " ");

        Assert.Throws<ArgumentException>(() => builder.ReceiveModule("fulfillment"));
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

    private static void ConfigureDurableModule<TCommand, THandler>(
        BondstoneModuleBuilder module)
        where TCommand : IDurableCommand
        where THandler : class, ICommandHandler<TCommand>
    {
        module.UseDurableMessaging();
        module.UsePersistence("test persistence");
        module.Commands.RegisterHandler<TCommand, THandler>();
    }

    [DurableCommandIdentity("fulfillment.order.reserve.v1")]
    public sealed record ReserveOrderCommand(string OrderId) : IDurableCommand;

    public sealed class ReserveOrderHandler : ICommandHandler<ReserveOrderCommand>
    {
        public ValueTask HandleAsync(
            ReserveOrderCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("billing.payment.capture.v1")]
    public sealed record CapturePaymentCommand(string PaymentId) : IDurableCommand;

    public sealed class CapturePaymentHandler : ICommandHandler<CapturePaymentCommand>
    {
        public ValueTask HandleAsync(
            CapturePaymentCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
