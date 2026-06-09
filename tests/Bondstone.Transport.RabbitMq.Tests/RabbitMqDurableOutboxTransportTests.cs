using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.RabbitMq.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.RabbitMq.Tests;

public sealed class RabbitMqDurableOutboxTransportTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenCommandIsClaimed_PublishesMessageToResolvedRoute()
    {
        var publisher = new RecordingRabbitMqMessagePublisher();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            publisher,
            rabbitMq =>
            {
                rabbitMq.UseCommandExchange("bondstone.commands");
                rabbitMq.RouteModule("fulfillment").ToRoutingKey("fulfillment.commands");
            });
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord());

        Assert.NotNull(publisher.Destination);
        Assert.Equal("bondstone.commands", publisher.Destination.ExchangeName);
        Assert.Equal("fulfillment.commands", publisher.Destination.RoutingKey);
        Assert.NotNull(publisher.Message);
        Assert.Equal(CreateRecord().Envelope.MessageId.ToString("D"), publisher.Message.MessageId);
        Assert.Equal("fulfillment.order.reserve.v1", publisher.Message.MessageTypeName);
        Assert.Equal(MessageKind.Command.ToString(), publisher.Message.Headers[
            BondstoneRabbitMqHeaders.MessageKind]);
        Assert.Equal("fulfillment", publisher.Message.Headers[
            BondstoneRabbitMqHeaders.TargetModule]);
        Assert.Contains("\"MessageKind\":\"Command\"", publisher.Message.Body, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenEventIsClaimed_PublishesMessageToResolvedRoute()
    {
        var publisher = new RecordingRabbitMqMessagePublisher();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            publisher,
            rabbitMq =>
            {
                rabbitMq.UseEventExchange("bondstone.events");
                rabbitMq.RouteEvent("sales.order.submitted.v1").ToRoutingKey("sales.order.submitted");
            });
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord(
            MessageKind.Event,
            targetModule: null,
            messageTypeName: "sales.order.submitted.v1"));

        Assert.NotNull(publisher.Destination);
        Assert.Equal("bondstone.events", publisher.Destination.ExchangeName);
        Assert.Equal("sales.order.submitted", publisher.Destination.RoutingKey);
        Assert.NotNull(publisher.Message);
        Assert.Equal("sales.order.submitted.v1", publisher.Message.MessageTypeName);
        Assert.False(publisher.Message.Headers.ContainsKey(BondstoneRabbitMqHeaders.TargetModule));
        Assert.Contains("\"MessageKind\":\"Event\"", publisher.Message.Body, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeCommandRoute_WhenConventionExists_ReturnsConventionRoute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());
        services.AddBondstone(
            bondstone => bondstone.UseRabbitMqTransport(rabbitMq =>
            {
                rabbitMq.UseCommandExchange("bondstone.commands");
                rabbitMq.UseModuleRoutingKeyConvention(static moduleName => $"module.{moduleName}");
            }));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRabbitMqTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRabbitMqTopologyDiagnostics>();

        RabbitMqCommandRoutingDiagnostic diagnostic =
            diagnostics.DescribeCommandRoute("fulfillment");

        Assert.Equal(RabbitMqCommandRoutingSource.RoutingKeyConvention, diagnostic.Source);
        Assert.Equal("bondstone.commands", diagnostic.ExchangeName);
        Assert.Equal("module.fulfillment", diagnostic.RoutingKey);
        Assert.True(diagnostic.HasRoute);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeEventRoute_WhenExchangeIsMissing_ReturnsMissingDiagnostic()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());
        services.AddBondstone(
            bondstone => bondstone.UseRabbitMqTransport(
                rabbitMq => rabbitMq.RouteEvent("sales.order.submitted.v1")
                    .ToRoutingKey("sales.order.submitted")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRabbitMqTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRabbitMqTopologyDiagnostics>();

        RabbitMqEventRoutingDiagnostic diagnostic =
            diagnostics.DescribeEventRoute("sales.order.submitted.v1");

        Assert.Equal(RabbitMqEventRoutingSource.Missing, diagnostic.Source);
        Assert.False(diagnostic.HasRoute);
        Assert.Contains("exchange", diagnostic.FailureReason, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider CreateServiceProvider(
        RecordingRabbitMqMessagePublisher publisher,
        Action<BondstoneRabbitMqTransportBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(publisher);
        services.AddBondstone(
            bondstone => bondstone.UseRabbitMqTransport(configure));

        return services.BuildServiceProvider();
    }

    private static DurableOutboxRecord CreateRecord(
        MessageKind messageKind = MessageKind.Command,
        string? targetModule = "fulfillment",
        string messageTypeName = "fulfillment.order.reserve.v1")
    {
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("3f1a9e26-75d4-4a7d-bb48-ae453f5e5e02"),
            messageKind,
            messageTypeName,
            "sales",
            targetModule,
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"),
            durableOperationId: Guid.Parse("5dac5be5-d1ef-432d-a5d5-597103ae44c9"),
            traceContext: new MessageTraceContext(
                "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"),
            causationId: Guid.Parse("a2d07b16-258d-4ad2-b310-1ef95d5c0936"),
            partitionKey: "orders/A-100");

        return new DurableOutboxRecord(
            envelope,
            DateTimeOffset.Parse("2026-06-09T12:00:01+00:00"),
            new DurableOutboxDispatchState(
                DurableOutboxStatus.Processing,
                attemptCount: 1,
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-09T12:05:00+00:00")));
    }

    private sealed class RecordingRabbitMqMessagePublisher : IRabbitMqMessagePublisher
    {
        public RabbitMqPublishDestination? Destination { get; private set; }

        public RabbitMqTransportMessage? Message { get; private set; }

        public ValueTask PublishAsync(
            RabbitMqPublishDestination destination,
            RabbitMqTransportMessage message,
            CancellationToken ct = default)
        {
            Destination = destination;
            Message = message;
            return ValueTask.CompletedTask;
        }
    }
}
