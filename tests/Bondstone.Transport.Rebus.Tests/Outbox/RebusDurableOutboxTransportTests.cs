using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Bus.Advanced;
using Rebus.Messages;
using Rebus.Routing;
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
        var transport = new RebusDurableOutboxTransport(
            routingApi,
            new RebusModuleDestinationResolver(
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
        var transport = new RebusDurableOutboxTransport(
            routingApi,
            new RebusModuleDestinationResolver(
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
        var transport = new RebusDurableOutboxTransport(
            routingApi,
            new RebusModuleDestinationResolver(
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
        var transport = new RebusDurableOutboxTransport(
            routingApi,
            new RebusModuleDestinationResolver(
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
        var transport = new RebusDurableOutboxTransport(
            routingApi,
            new RebusModuleDestinationResolver(
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
        var transport = new RebusDurableOutboxTransport(
            routingApi,
            new RebusModuleDestinationResolver(
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
    public async Task SendAsync_WhenEnvelopeIsEvent_Throws()
    {
        DurableOutboxRecord record = CreateRecord(
            messageKind: MessageKind.Event,
            targetModule: null);
        var transport = new RebusDurableOutboxTransport(
            new RecordingRoutingApi(),
            new RebusModuleDestinationResolver(new Dictionary<string, string>()));

        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await transport.SendAsync(record));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenNoDestinationIsConfiguredForTargetModule_Throws()
    {
        DurableOutboxRecord record = CreateRecord();
        var transport = new RebusDurableOutboxTransport(
            new RecordingRoutingApi(),
            new RebusModuleDestinationResolver(new Dictionary<string, string>()));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await transport.SendAsync(record));

        Assert.Contains("fulfillment", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AddBondstoneRebusOutboxTransport_RegistersTransportWithModuleDestinations()
    {
        var routingApi = new RecordingRoutingApi();
        var services = new ServiceCollection();
        services.AddSingleton<IRoutingApi>(routingApi);
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
        services.AddSingleton<IRoutingApi>(routingApi);
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
        services.AddSingleton<IRoutingApi>(routingApi);
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment")));

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
        services.AddSingleton<IRoutingApi>(routingApi);
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus =>
                {
                    rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");
                    rebus.RouteModule("fulfillment").ToQueue("fulfillment-priority");
                }));

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
        services.AddSingleton<IRoutingApi>(routingApi);
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus
                    .UseModuleQueueConvention()
                    .ReceiveModule("fulfillment")));

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
        services.AddSingleton<IRoutingApi>(routingApi);
        services.AddBondstone(
            bondstone => bondstone.UseRebusTransport(
                rebus => rebus.UseModuleQueueConvention()));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord());

        Assert.Equal("fulfillment-commands", routingApi.DestinationAddress);
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
}
