using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.ServiceBus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.ServiceBus.Tests;

public sealed class ServiceBusDurableOutboxTransportTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenCommandIsClaimed_SendsMessageToResolvedQueue()
    {
        var sender = new RecordingServiceBusMessageSender();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            sender,
            serviceBus => serviceBus.RouteModule("fulfillment").ToQueue("fulfillment-commands"));
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord());

        Assert.Equal("fulfillment-commands", sender.EntityName);
        Assert.NotNull(sender.Message);
        Assert.Equal(CreateRecord().Envelope.MessageId.ToString("D"), sender.Message.MessageId);
        Assert.Equal("fulfillment.order.reserve.v1", sender.Message.Subject);
        Assert.Equal("orders/A-100", sender.Message.PartitionKey);
        Assert.Equal(MessageKind.Command.ToString(), sender.Message.ApplicationProperties[
            BondstoneServiceBusHeaders.MessageKind]);
        Assert.Equal("fulfillment", sender.Message.ApplicationProperties[
            BondstoneServiceBusHeaders.TargetModule]);
        Assert.Contains("\"MessageKind\":\"Command\"", sender.Message.Body, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenEventIsClaimed_SendsMessageToResolvedTopic()
    {
        var sender = new RecordingServiceBusMessageSender();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            sender,
            serviceBus => serviceBus.RouteEvent("sales.order.submitted.v1").ToTopic("sales-events"));
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord(
            MessageKind.Event,
            targetModule: null,
            messageTypeName: "sales.order.submitted.v1"));

        Assert.Equal("sales-events", sender.EntityName);
        Assert.NotNull(sender.Message);
        Assert.Equal("sales.order.submitted.v1", sender.Message.Subject);
        Assert.False(sender.Message.ApplicationProperties.ContainsKey(
            BondstoneServiceBusHeaders.TargetModule));
        Assert.Contains("\"MessageKind\":\"Event\"", sender.Message.Body, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenEventIsClaimed_SendsMessageToResolvedQueue()
    {
        var sender = new RecordingServiceBusMessageSender();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            sender,
            serviceBus => serviceBus.RouteEvent("sales.order.submitted.v1").ToQueue("sales-events"));
        IDurableOutboxTransport transport =
            serviceProvider.GetRequiredService<IDurableOutboxTransport>();

        await transport.SendAsync(CreateRecord(
            MessageKind.Event,
            targetModule: null,
            messageTypeName: "sales.order.submitted.v1"));

        Assert.Equal("sales-events", sender.EntityName);
        Assert.NotNull(sender.Message);
        Assert.Equal("sales.order.submitted.v1", sender.Message.Subject);
        Assert.False(sender.Message.ApplicationProperties.ContainsKey(
            BondstoneServiceBusHeaders.TargetModule));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeCommandDestination_WhenConventionExists_ReturnsQueueConvention()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());
        services.AddBondstone(
            bondstone => bondstone.UseServiceBusTransport(
                serviceBus => serviceBus.UseModuleQueueConvention(
                    static moduleName => $"module-{moduleName}")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceBusTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IServiceBusTopologyDiagnostics>();

        ServiceBusCommandDestinationDiagnostic diagnostic =
            diagnostics.DescribeCommandDestination("fulfillment");

        Assert.Equal(ServiceBusCommandDestinationSource.QueueConvention, diagnostic.Source);
        Assert.Equal("module-fulfillment", diagnostic.QueueName);
        Assert.True(diagnostic.HasDestination);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeEventDestination_WhenDestinationIsMissing_ReturnsMissingDiagnostic()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());
        services.AddBondstone(
            bondstone => bondstone.UseServiceBusTransport(_ => { }));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceBusTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IServiceBusTopologyDiagnostics>();

        ServiceBusEventDestinationDiagnostic diagnostic =
            diagnostics.DescribeEventDestination("sales.order.submitted.v1");

        Assert.Equal(ServiceBusEventDestinationSource.Missing, diagnostic.Source);
        Assert.False(diagnostic.HasDestination);
        Assert.Contains("sales.order.submitted.v1", diagnostic.FailureReason, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateServiceProvider(
        RecordingServiceBusMessageSender sender,
        Action<BondstoneServiceBusTransportBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(sender);
        services.AddBondstone(
            bondstone => bondstone.UseServiceBusTransport(configure));

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

    private sealed class RecordingServiceBusMessageSender : IServiceBusMessageSender
    {
        public string? EntityName { get; private set; }

        public ServiceBusTransportMessage? Message { get; private set; }

        public ValueTask SendAsync(
            string entityName,
            ServiceBusTransportMessage message,
            CancellationToken ct = default)
        {
            EntityName = entityName;
            Message = message;
            return ValueTask.CompletedTask;
        }
    }
}
