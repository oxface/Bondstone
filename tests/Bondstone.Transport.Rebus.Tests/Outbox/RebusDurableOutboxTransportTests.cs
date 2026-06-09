using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Messages;
using Rebus.Routing;
using System.Reflection;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Outbox;

public sealed class RebusDurableOutboxTransportTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenCommandIsClaimed_RoutesWireEnvelopeToResolvedDestination()
    {
        DurableOutboxRecord record = CreateRecord();
        var routingApi = new RecordingRoutingApi();
        var transport = CreateTransport(
            routingApi,
            destinationResolver: new RebusModuleDestinationResolver(
                new Dictionary<string, string>
                {
                    ["fulfillment"] = "fulfillment-queue",
                }));

        await transport.SendAsync(record);

        Assert.Equal("fulfillment-queue", routingApi.DestinationAddress);

        RebusDurableMessageEnvelope rebusEnvelope =
            Assert.IsType<RebusDurableMessageEnvelope>(routingApi.Message);
        Assert.Equal(record.Envelope.MessageId, rebusEnvelope.MessageId);
        Assert.Equal(MessageKind.Command.ToString(), rebusEnvelope.MessageKind);
        Assert.Equal(record.Envelope.MessageTypeName, rebusEnvelope.MessageTypeName);
        Assert.Equal(record.Envelope.SourceModule, rebusEnvelope.SourceModule);
        Assert.Equal(record.Envelope.TargetModule, rebusEnvelope.TargetModule);
        Assert.Equal(record.Envelope.Payload, rebusEnvelope.Payload);
        Assert.Equal(record.Envelope.Metadata, rebusEnvelope.Metadata);
        Assert.Equal(record.Envelope.CreatedAtUtc, rebusEnvelope.CreatedAtUtc);
        Assert.Equal(record.Envelope.DurableOperationId, rebusEnvelope.DurableOperationId);
        Assert.Equal(record.Envelope.TraceContext?.TraceParent, rebusEnvelope.TraceParent);
        Assert.Equal(record.Envelope.TraceContext?.TraceState, rebusEnvelope.TraceState);
        Assert.Equal(record.Envelope.TraceContext?.Baggage, rebusEnvelope.TraceBaggage);
        Assert.Equal(record.Envelope.CausationId, rebusEnvelope.CausationId);
        Assert.Equal(record.Envelope.PartitionKey, rebusEnvelope.PartitionKey);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenTraceContextExists_MapsDurableAndTraceHeaders()
    {
        DurableOutboxRecord record = CreateRecord();
        var routingApi = new RecordingRoutingApi();
        var transport = CreateTransport(
            routingApi,
            destinationResolver: new RebusModuleDestinationResolver(
                new Dictionary<string, string>
                {
                    ["fulfillment"] = "fulfillment-queue",
                }));

        await transport.SendAsync(record);

        Assert.NotNull(routingApi.Headers);
        IReadOnlyDictionary<string, string> headers = routingApi.Headers;
        Assert.Equal(record.Envelope.MessageId.ToString("D"), headers[Headers.MessageId]);
        Assert.Equal("4bf92f3577b34da6a3ce929d0e0e4736", headers[Headers.CorrelationId]);
        Assert.Equal(record.Envelope.CausationId?.ToString("D"), headers[Headers.InReplyTo]);
        Assert.Equal(record.Envelope.MessageId.ToString("D"), headers[BondstoneRebusHeaders.MessageId]);
        Assert.Equal(MessageKind.Command.ToString(), headers[BondstoneRebusHeaders.MessageKind]);
        Assert.Equal(record.Envelope.MessageTypeName, headers[BondstoneRebusHeaders.MessageTypeName]);
        Assert.Equal(record.Envelope.SourceModule, headers[BondstoneRebusHeaders.SourceModule]);
        Assert.Equal(record.Envelope.TargetModule, headers[BondstoneRebusHeaders.TargetModule]);
        Assert.Equal(record.Envelope.DurableOperationId?.ToString("D"), headers[BondstoneRebusHeaders.DurableOperationId]);
        Assert.Equal(record.Envelope.CausationId?.ToString("D"), headers[BondstoneRebusHeaders.CausationId]);
        Assert.Equal(record.Envelope.PartitionKey, headers[BondstoneRebusHeaders.PartitionKey]);
        Assert.Equal(record.Envelope.TraceContext?.TraceParent, headers[BondstoneRebusHeaders.TraceParent]);
        Assert.Equal(record.Envelope.TraceContext?.TraceState, headers[BondstoneRebusHeaders.TraceState]);
        Assert.Equal(record.Envelope.TraceContext?.Baggage, headers[BondstoneRebusHeaders.Baggage]);
        Assert.False(headers.ContainsKey(Headers.Type));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenDestinationConventionExists_RoutesTargetModuleByConvention()
    {
        DurableOutboxRecord record = CreateRecord();
        var routingApi = new RecordingRoutingApi();
        var transport = CreateTransport(
            routingApi,
            destinationResolver: new RebusModuleDestinationResolver(
                new Dictionary<string, string>(),
                static targetModule => $"module-{targetModule}"));

        await transport.SendAsync(record);

        Assert.Equal("module-fulfillment", routingApi.DestinationAddress);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenExplicitDestinationAndConventionExist_UsesExplicitDestination()
    {
        DurableOutboxRecord record = CreateRecord();
        var routingApi = new RecordingRoutingApi();
        var transport = CreateTransport(
            routingApi,
            destinationResolver: new RebusModuleDestinationResolver(
                new Dictionary<string, string>
                {
                    ["fulfillment"] = "fulfillment-priority",
                },
                static targetModule => $"module-{targetModule}"));

        await transport.SendAsync(record);

        Assert.Equal("fulfillment-priority", routingApi.DestinationAddress);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenTraceContextIsMissing_UsesDurableOperationIdAsCorrelationId()
    {
        DurableOutboxRecord record = CreateRecord(includeTraceContext: false);
        var routingApi = new RecordingRoutingApi();
        var transport = CreateTransport(
            routingApi,
            destinationResolver: new RebusModuleDestinationResolver(
                new Dictionary<string, string>
                {
                    ["fulfillment"] = "fulfillment-queue",
                }));

        await transport.SendAsync(record);

        Assert.NotNull(routingApi.Headers);
        Assert.Equal(
            record.Envelope.DurableOperationId?.ToString("D"),
            routingApi.Headers[Headers.CorrelationId]);
        Assert.False(routingApi.Headers.ContainsKey(BondstoneRebusHeaders.TraceParent));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenTraceParentIsNotW3C_UsesDurableOperationIdAsCorrelationId()
    {
        DurableOutboxRecord record = CreateRecord(
            traceContext: new MessageTraceContext("legacy-correlation-id"));
        var routingApi = new RecordingRoutingApi();
        var transport = CreateTransport(
            routingApi,
            destinationResolver: new RebusModuleDestinationResolver(
                new Dictionary<string, string>
                {
                    ["fulfillment"] = "fulfillment-queue",
                }));

        await transport.SendAsync(record);

        Assert.NotNull(routingApi.Headers);
        Assert.Equal(
            record.Envelope.DurableOperationId?.ToString("D"),
            routingApi.Headers[Headers.CorrelationId]);
        Assert.Equal("legacy-correlation-id", routingApi.Headers[BondstoneRebusHeaders.TraceParent]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenEventIsClaimed_PublishesWireEnvelopeToResolvedTopic()
    {
        DurableOutboxRecord record = CreateRecord(
            messageKind: MessageKind.Event,
            targetModule: null);
        var routingApi = new RecordingRoutingApi();
        var topicsApi = new RecordingTopicsApi();
        var transport = CreateTransport(
            routingApi,
            topicsApi: topicsApi,
            eventTopicResolver: new RebusEventTopicResolver(
                new Dictionary<string, string>
                {
                    ["sales.order.submit.v1"] = "sales.order.events",
                }));

        await transport.SendAsync(record);

        Assert.Null(routingApi.DestinationAddress);
        Assert.Equal("sales.order.events", topicsApi.TopicName);

        RebusDurableMessageEnvelope rebusEnvelope =
            Assert.IsType<RebusDurableMessageEnvelope>(topicsApi.Message);
        Assert.Equal(record.Envelope.MessageId, rebusEnvelope.MessageId);
        Assert.Equal(MessageKind.Event.ToString(), rebusEnvelope.MessageKind);
        Assert.Equal(record.Envelope.MessageTypeName, rebusEnvelope.MessageTypeName);
        Assert.Equal(record.Envelope.SourceModule, rebusEnvelope.SourceModule);
        Assert.Null(rebusEnvelope.TargetModule);
        Assert.Equal(record.Envelope.Payload, rebusEnvelope.Payload);
        Assert.Equal(record.Envelope.Metadata, rebusEnvelope.Metadata);

        Assert.NotNull(topicsApi.Headers);
        IReadOnlyDictionary<string, string> headers = topicsApi.Headers;
        Assert.Equal(record.Envelope.MessageId.ToString("D"), headers[Headers.MessageId]);
        Assert.Equal(MessageKind.Event.ToString(), headers[BondstoneRebusHeaders.MessageKind]);
        Assert.Equal(record.Envelope.MessageTypeName, headers[BondstoneRebusHeaders.MessageTypeName]);
        Assert.Equal(record.Envelope.SourceModule, headers[BondstoneRebusHeaders.SourceModule]);
        Assert.False(headers.ContainsKey(BondstoneRebusHeaders.TargetModule));
        Assert.False(headers.ContainsKey(Headers.Type));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenEventTopicIsMissing_Throws()
    {
        DurableOutboxRecord record = CreateRecord(
            messageKind: MessageKind.Event,
            targetModule: null);
        var transport = CreateTransport(
            new RecordingRoutingApi(),
            topicsApi: new RecordingTopicsApi(),
            eventTopicResolver: new RebusEventTopicResolver(new Dictionary<string, string>()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await transport.SendAsync(record));

        Assert.Contains("sales.order.submit.v1", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenNoDestinationIsConfiguredForTargetModule_Throws()
    {
        DurableOutboxRecord record = CreateRecord();
        var transport = CreateTransport(
            new RecordingRoutingApi(),
            destinationResolver: new RebusModuleDestinationResolver(new Dictionary<string, string>()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await transport.SendAsync(record));

        Assert.Contains("fulfillment", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeCommandDestination_WhenExplicitRouteExists_ReturnsExplicitRoute()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.RouteModule("fulfillment").ToQueue("fulfillment-queue")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusCommandTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusCommandTopologyDiagnostics>();

        RebusCommandDestinationDiagnostic diagnostic =
            diagnostics.DescribeCommandDestination("fulfillment");

        Assert.Equal(DurableMessageTopologyDiagnosticKind.CommandDestination, diagnostic.Kind);
        Assert.Equal("fulfillment", diagnostic.TargetModule);
        Assert.Equal(RebusCommandDestinationSource.ExplicitRoute, diagnostic.Source);
        Assert.Equal("fulfillment-queue", diagnostic.DestinationAddress);
        Assert.Null(diagnostic.ReceiveEndpointName);
        Assert.Null(diagnostic.FailureReason);
        Assert.True(diagnostic.HasDestination);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeCommandDestination_WhenExplicitRouteAndReceiveEndpointExist_ReturnsExplicitRoute()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone =>
            {
                bondstone.Module("fulfillment", ConfigureFulfillmentModule);
                bondstone.UseRebusTransport(
                    rebus =>
                    {
                        rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");
                        rebus.RouteModule("fulfillment").ToQueue("fulfillment-priority");
                    });
            });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusCommandTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusCommandTopologyDiagnostics>();

        RebusCommandDestinationDiagnostic diagnostic =
            diagnostics.DescribeCommandDestination("fulfillment");

        Assert.Equal(RebusCommandDestinationSource.ExplicitRoute, diagnostic.Source);
        Assert.Equal("fulfillment-priority", diagnostic.DestinationAddress);
        Assert.Null(diagnostic.ReceiveEndpointName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeCommandDestination_WhenReceiveEndpointAcceptsModule_ReturnsReceiveEndpoint()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone =>
            {
                bondstone.Module("fulfillment", ConfigureFulfillmentModule);
                bondstone.UseRebusTransport(
                    rebus => rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment"));
            });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusCommandTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusCommandTopologyDiagnostics>();

        RebusCommandDestinationDiagnostic diagnostic =
            diagnostics.DescribeCommandDestination("fulfillment");

        Assert.Equal(RebusCommandDestinationSource.ReceiveEndpoint, diagnostic.Source);
        Assert.Equal("fulfillment-commands", diagnostic.DestinationAddress);
        Assert.Equal("fulfillment-commands", diagnostic.ReceiveEndpointName);
        Assert.Null(diagnostic.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeCommandDestination_WhenConventionExists_ReturnsModuleQueueConvention()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.UseModuleQueueConvention(
                    static moduleName => $"module-{moduleName}")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusCommandTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusCommandTopologyDiagnostics>();

        RebusCommandDestinationDiagnostic diagnostic =
            diagnostics.DescribeCommandDestination("fulfillment");

        Assert.Equal(RebusCommandDestinationSource.ModuleQueueConvention, diagnostic.Source);
        Assert.Equal("module-fulfillment", diagnostic.DestinationAddress);
        Assert.Null(diagnostic.ReceiveEndpointName);
        Assert.Null(diagnostic.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeCommandDestination_WhenDestinationIsMissing_ReturnsMissingDiagnostic()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(_ => { }));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusCommandTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusCommandTopologyDiagnostics>();

        RebusCommandDestinationDiagnostic diagnostic =
            diagnostics.DescribeCommandDestination("fulfillment");

        Assert.Equal(RebusCommandDestinationSource.Missing, diagnostic.Source);
        Assert.Null(diagnostic.DestinationAddress);
        Assert.Null(diagnostic.ReceiveEndpointName);
        Assert.Contains("fulfillment", diagnostic.FailureReason, StringComparison.Ordinal);
        Assert.False(diagnostic.HasDestination);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeEventTopic_WhenExplicitRouteExists_ReturnsExplicitRoute()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.RouteEvent("sales.order.submit.v1").ToTopic("sales.order.events")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusEventTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusEventTopologyDiagnostics>();

        RebusEventTopicDiagnostic diagnostic =
            diagnostics.DescribeEventTopic("sales.order.submit.v1");

        Assert.Equal(DurableMessageTopologyDiagnosticKind.EventTopic, diagnostic.Kind);
        Assert.Equal("sales.order.submit.v1", diagnostic.MessageTypeName);
        Assert.Equal(RebusEventTopicSource.ExplicitRoute, diagnostic.Source);
        Assert.Equal("sales.order.events", diagnostic.TopicName);
        Assert.Null(diagnostic.FailureReason);
        Assert.True(diagnostic.HasTopic);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeEventTopic_WhenConventionExists_ReturnsConventionTopic()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.UseEventTopicConvention(
                    static messageTypeName => $"topic.{messageTypeName}")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusEventTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusEventTopologyDiagnostics>();

        RebusEventTopicDiagnostic diagnostic =
            diagnostics.DescribeEventTopic("sales.order.submit.v1");

        Assert.Equal(RebusEventTopicSource.Convention, diagnostic.Source);
        Assert.Equal("topic.sales.order.submit.v1", diagnostic.TopicName);
        Assert.Null(diagnostic.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeEventTopic_WhenTopicIsMissing_ReturnsMissingDiagnostic()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(_ => { }));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusEventTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusEventTopologyDiagnostics>();

        RebusEventTopicDiagnostic diagnostic =
            diagnostics.DescribeEventTopic("sales.order.submit.v1");

        Assert.Equal(RebusEventTopicSource.Missing, diagnostic.Source);
        Assert.Null(diagnostic.TopicName);
        Assert.Contains("sales.order.submit.v1", diagnostic.FailureReason, StringComparison.Ordinal);
        Assert.False(diagnostic.HasTopic);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeEventSubscriptions_WhenSubscriberBindingExists_ReturnsSubscribersAndTopic()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone =>
            {
                bondstone.Module("fulfillment", ConfigureFulfillmentEventSubscriberModule);
                bondstone.UseRebusTransport(rebus =>
                {
                    rebus.RouteEvent("sales.order.submit.v1").ToTopic("sales.order.events");
                    rebus.ReceiveEndpoint("fulfillment-events").SubscribeEvent(
                        "sales.order.submit.v1",
                        "fulfillment",
                        "fulfillment.order-projection.v1");
                });
            });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusEventTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusEventTopologyDiagnostics>();

        RebusEventSubscriptionDiagnostic diagnostic =
            diagnostics.DescribeEventSubscriptions("sales.order.submit.v1");

        Assert.Equal(DurableMessageTopologyDiagnosticKind.EventSubscription, diagnostic.Kind);
        Assert.Equal("sales.order.submit.v1", diagnostic.MessageTypeName);
        Assert.True(diagnostic.HasSubscriptions);
        Assert.Null(diagnostic.FailureReason);
        Assert.Equal(RebusEventTopicSource.ExplicitRoute, diagnostic.Topic.Source);
        Assert.Equal("sales.order.events", diagnostic.Topic.TopicName);

        RebusEventSubscriberDiagnostic subscriber = Assert.Single(diagnostic.Subscribers);
        Assert.Equal("fulfillment-events", subscriber.EndpointName);
        Assert.Equal("fulfillment", subscriber.SubscriberModule);
        Assert.Equal("fulfillment.order-projection.v1", subscriber.SubscriberIdentity);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeEventSubscriptions_WhenNoSubscriberBindingExists_ReturnsMissingDiagnostic()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.RouteEvent("sales.order.submit.v1").ToTopic("sales.order.events")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusEventTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusEventTopologyDiagnostics>();

        RebusEventSubscriptionDiagnostic diagnostic =
            diagnostics.DescribeEventSubscriptions("sales.order.submit.v1");

        Assert.False(diagnostic.HasSubscriptions);
        Assert.Empty(diagnostic.Subscribers);
        Assert.Equal("sales.order.events", diagnostic.Topic.TopicName);
        Assert.Contains("sales.order.submit.v1", diagnostic.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeEventSubscriptions_WhenTopicIsMissing_ReturnsSubscribersAndMissingTopic()
    {
        var services = new ServiceCollection();
        services.AddBondstone(
            bondstone =>
            {
                bondstone.Module("fulfillment", ConfigureFulfillmentEventSubscriberModule);
                bondstone.UseRebusTransport(rebus =>
                {
                    rebus.ReceiveEndpoint("fulfillment-events").SubscribeEvent(
                        "sales.order.submit.v1",
                        "fulfillment",
                        "fulfillment.order-projection.v1");
                });
            });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRebusEventTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRebusEventTopologyDiagnostics>();

        RebusEventSubscriptionDiagnostic diagnostic =
            diagnostics.DescribeEventSubscriptions("sales.order.submit.v1");

        Assert.True(diagnostic.HasSubscriptions);
        Assert.Null(diagnostic.FailureReason);
        Assert.Equal(RebusEventTopicSource.Missing, diagnostic.Topic.Source);
        Assert.Null(diagnostic.Topic.TopicName);
        Assert.Contains("sales.order.submit.v1", diagnostic.Topic.FailureReason, StringComparison.Ordinal);
        Assert.Single(diagnostic.Subscribers);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AddBondstoneRebusOutboxTransport_RegistersTransportWithModuleDestinations()
    {
        var routingApi = new RecordingRoutingApi();
        var services = new ServiceCollection();
        services.AddSingleton(CreateBus(routingApi));
        services.AddBondstoneRebusOutboxTransport(
            new Dictionary<string, string>
            {
                ["fulfillment"] = "fulfillment-queue",
            });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDurableOutboxTransport transport = serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord());

        Assert.Equal("fulfillment-queue", routingApi.DestinationAddress);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UseRebusTransport_WhenConfiguredWithTopologyBuilder_RoutesConfiguredModule()
    {
        var routingApi = new RecordingRoutingApi();
        var services = new ServiceCollection();
        services.AddSingleton(CreateBus(routingApi));
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.RouteModule("fulfillment").ToQueue("fulfillment-queue")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord());

        Assert.Equal("fulfillment-queue", routingApi.DestinationAddress);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UseRebusTransport_WhenReceiveEndpointAcceptsModule_DerivesOutgoingRoute()
    {
        var routingApi = new RecordingRoutingApi();
        var services = new ServiceCollection();
        services.AddSingleton(CreateBus(routingApi));
        services.AddBondstone(
            bondstone =>
            {
                bondstone.Module("fulfillment", ConfigureFulfillmentModule);
                bondstone.UseRebusTransport(
                    rebus => rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment"));
            });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord());

        Assert.Equal("fulfillment-commands", routingApi.DestinationAddress);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UseRebusTransport_WhenExplicitRouteAndReceiveEndpointExist_UsesExplicitRoute()
    {
        var routingApi = new RecordingRoutingApi();
        var services = new ServiceCollection();
        services.AddSingleton(CreateBus(routingApi));
        services.AddBondstone(
            bondstone =>
            {
                bondstone.Module("fulfillment", ConfigureFulfillmentModule);
                bondstone.UseRebusTransport(
                    rebus =>
                    {
                        rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");
                        rebus.RouteModule("fulfillment").ToQueue("fulfillment-priority");
                    });
            });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord());

        Assert.Equal("fulfillment-priority", routingApi.DestinationAddress);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UseRebusTransport_WhenReceiveModuleUsesConvention_RoutesTargetModuleByConvention()
    {
        var routingApi = new RecordingRoutingApi();
        var services = new ServiceCollection();
        services.AddSingleton(CreateBus(routingApi));
        services.AddBondstone(
            bondstone =>
            {
                bondstone.Module("fulfillment", ConfigureFulfillmentModule);
                bondstone.UseRebusTransport(
                    rebus => rebus
                        .UseModuleQueueConvention()
                        .ReceiveModule("fulfillment"));
            });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord());

        Assert.Equal("fulfillment-commands", routingApi.DestinationAddress);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UseRebusTransport_WhenOnlyConventionIsConfigured_RoutesTargetModuleByConvention()
    {
        var routingApi = new RecordingRoutingApi();
        var services = new ServiceCollection();
        services.AddSingleton(CreateBus(routingApi));
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.UseModuleQueueConvention()));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord());

        Assert.Equal("fulfillment-commands", routingApi.DestinationAddress);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UseRebusTransport_WhenEventTopicIsConfigured_PublishesEventToTopic()
    {
        var routingApi = new RecordingRoutingApi();
        var topicsApi = new RecordingTopicsApi();
        var services = new ServiceCollection();
        services.AddSingleton(CreateBus(routingApi, topicsApi));
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.RouteEvent("sales.order.submit.v1").ToTopic("sales.order.events")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord(
            messageKind: MessageKind.Event,
            targetModule: null));

        Assert.Null(routingApi.DestinationAddress);
        Assert.Equal("sales.order.events", topicsApi.TopicName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UseRebusTransport_WhenEventTopicConventionIsConfigured_PublishesEventByIdentity()
    {
        var topicsApi = new RecordingTopicsApi();
        var services = new ServiceCollection();
        services.AddSingleton(CreateBus(new RecordingRoutingApi(), topicsApi));
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.UseEventTopicConvention()));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord(
            messageKind: MessageKind.Event,
            targetModule: null));

        Assert.Equal("sales.order.submit.v1", topicsApi.TopicName);
    }

    private static DurableOutboxRecord CreateRecord(
        MessageKind messageKind = MessageKind.Command,
        string? targetModule = "fulfillment",
        bool includeTraceContext = true,
        MessageTraceContext? traceContext = null)
    {
        traceContext ??= includeTraceContext
            ? new MessageTraceContext(
                "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
                "state=value",
                "tenant=sales")
            : null;

        var envelope = new DurableMessageEnvelope(
            Guid.Parse("48cb19e0-3689-4ec7-b629-8f8e19916d43"),
            messageKind,
            "sales.order.submit.v1",
            "sales",
            targetModule,
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"),
            durableOperationId: Guid.Parse("a0e7c46f-2699-40ec-888a-267b9323a164"),
            traceContext: traceContext,
            causationId: Guid.Parse("e01d0600-18dd-4573-9947-5c6a72eca8ab"),
            partitionKey: "orders/A-100",
            metadata: """{"schema":"sales.order.submit"}""");

        return new DurableOutboxRecord(
            envelope,
            DateTimeOffset.Parse("2026-06-04T00:00:01+00:00"),
            new DurableOutboxDispatchState(
                DurableOutboxStatus.Processing,
                attemptCount: 1,
                claimedBy: "dispatcher-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-04T00:05:00+00:00")));
    }

    private static void ConfigureFulfillmentModule(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.UsePersistence("test persistence");
        module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
    }

    private static void ConfigureFulfillmentEventSubscriberModule(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.UsePersistence("test persistence");
        module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderSubmittedHandler>(
            "fulfillment.order-projection.v1");
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

    [IntegrationEventIdentity("sales.order.submit.v1")]
    public sealed record OrderSubmittedEvent(string OrderId) : IIntegrationEvent;

    public sealed class OrderSubmittedHandler : IIntegrationEventHandler<OrderSubmittedEvent>
    {
        public ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private static RebusDurableOutboxTransport CreateTransport(
        RecordingRoutingApi routingApi,
        IRebusOutboxDestinationResolver? destinationResolver = null,
        RecordingTopicsApi? topicsApi = null,
        IRebusOutboxEventTopicResolver? eventTopicResolver = null)
    {
        return new RebusDurableOutboxTransport(
            CreateBus(routingApi, topicsApi),
            destinationResolver ?? new RebusModuleDestinationResolver(new Dictionary<string, string>()),
            eventTopicResolver ?? new RebusEventTopicResolver(new Dictionary<string, string>()));
    }

    private static IBus CreateBus(
        RecordingRoutingApi routingApi,
        RecordingTopicsApi? topicsApi = null)
    {
        IBus bus = DispatchProxy.Create<IBus, RebusBusProxy>();
        var proxy = (RebusBusProxy)(object)bus;
        proxy.RoutingApi = routingApi;
        proxy.TopicsApi = topicsApi ?? new RecordingTopicsApi();
        return bus;
    }

    private class RebusBusProxy : DispatchProxy
    {
        public RecordingRoutingApi RoutingApi { get; set; } = null!;

        public RecordingTopicsApi TopicsApi { get; set; } = null!;

        protected override object? Invoke(
            MethodInfo? targetMethod,
            object?[]? args)
        {
            if (targetMethod?.Name == "get_Advanced")
            {
                return CreateAdvancedApi(RoutingApi, TopicsApi);
            }

            if (targetMethod?.Name == nameof(IDisposable.Dispose))
            {
                return null;
            }

            if (targetMethod?.ReturnType == typeof(Task))
            {
                return Task.CompletedTask;
            }

            throw new NotSupportedException(
                $"Rebus bus member '{targetMethod?.Name}' is not supported by this test proxy.");
        }
    }

    private static IAdvancedApi CreateAdvancedApi(
        RecordingRoutingApi routingApi,
        RecordingTopicsApi topicsApi)
    {
        IAdvancedApi advancedApi =
            DispatchProxy.Create<IAdvancedApi, RebusAdvancedApiProxy>();
        var proxy = (RebusAdvancedApiProxy)(object)advancedApi;
        proxy.RoutingApi = routingApi;
        proxy.TopicsApi = topicsApi;
        return advancedApi;
    }

    private class RebusAdvancedApiProxy : DispatchProxy
    {
        public RecordingRoutingApi RoutingApi { get; set; } = null!;

        public RecordingTopicsApi TopicsApi { get; set; } = null!;

        protected override object? Invoke(
            MethodInfo? targetMethod,
            object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_Routing" => RoutingApi,
                "get_Topics" => TopicsApi,
                _ => throw new NotSupportedException(
                    $"Rebus advanced API member '{targetMethod?.Name}' is not supported by this test proxy."),
            };
        }
    }

    private sealed class RecordingRoutingApi : IRoutingApi
    {
        public string? DestinationAddress { get; private set; }

        public object? Message { get; private set; }

        public IReadOnlyDictionary<string, string> Headers { get; private set; } =
            new Dictionary<string, string>();

        public Task Send(
            string destinationAddress,
            object explicitlyRoutedMessage,
            IDictionary<string, string> optionalHeaders = null!)
        {
            DestinationAddress = destinationAddress;
            Message = explicitlyRoutedMessage;
            Headers = new Dictionary<string, string>(optionalHeaders, StringComparer.Ordinal);
            return Task.CompletedTask;
        }

        public Task SendRoutingSlip(
            Itinerary itinerary,
            object message,
            IDictionary<string, string> optionalHeaders = null!)
        {
            throw new NotSupportedException();
        }

        public Task Defer(
            string destinationAddress,
            TimeSpan delay,
            object message,
            IDictionary<string, string> optionalHeaders = null!)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingTopicsApi : ITopicsApi
    {
        public string? TopicName { get; private set; }

        public object? Message { get; private set; }

        public IReadOnlyDictionary<string, string> Headers { get; private set; } =
            new Dictionary<string, string>();

        public Task Publish(
            string topic,
            object message,
            IDictionary<string, string> optionalHeaders = null!)
        {
            TopicName = topic;
            Message = message;
            Headers = new Dictionary<string, string>(optionalHeaders, StringComparer.Ordinal);
            return Task.CompletedTask;
        }

        public Task Subscribe(string topic)
        {
            throw new NotSupportedException();
        }

        public Task Unsubscribe(string topic)
        {
            throw new NotSupportedException();
        }
    }
}
