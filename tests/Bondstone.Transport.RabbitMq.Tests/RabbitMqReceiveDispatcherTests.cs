using System.Text.Json;
using System.Text;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.RabbitMq.Inbox;
using Bondstone.Transport.RabbitMq.Outbox;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Bondstone.Transport.RabbitMq.Tests;

public sealed class RabbitMqReceiveDispatcherTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenCommandQueueAcceptsModule_DispatchesCommand()
    {
        var commandPipeline = new RecordingCommandReceivePipeline();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            commandPipeline,
            new RecordingEventReceivePipeline(),
            rabbitMq =>
                rabbitMq.ReceiveQueue("fulfillment.commands")
                    .AcceptModule("fulfillment"));
        IRabbitMqReceivedMessageDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRabbitMqReceivedMessageDispatcher>();
        RabbitMqTransportMessage message = CreateMessage();

        await dispatcher.DispatchAsync("fulfillment.commands", message);

        Assert.Equal(1, commandPipeline.HandledCount);
        Assert.NotNull(commandPipeline.Envelope);
        Assert.Equal("fulfillment", commandPipeline.Envelope.TargetModule);
        Assert.Equal("fulfillment.order.reserve.v1", commandPipeline.Envelope.MessageTypeName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenEventQueueHasSubscribers_DispatchesEachSubscriber()
    {
        var eventPipeline = new RecordingEventReceivePipeline();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            eventPipeline,
            rabbitMq =>
                rabbitMq.ReceiveQueue("sales-events")
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "fulfillment",
                        "fulfillment.sales-order-projection.v1")
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "billing",
                        "billing.sales-order-projection.v1"));
        IRabbitMqReceivedMessageDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRabbitMqReceivedMessageDispatcher>();
        RabbitMqTransportMessage message = CreateMessage(
            MessageKind.Event,
            targetModule: null,
            messageTypeName: "sales.order.submitted.v1");

        await dispatcher.DispatchAsync("sales-events", message);

        Assert.Equal(2, eventPipeline.Deliveries.Count);
        Assert.Contains(
            eventPipeline.Deliveries,
            delivery =>
                delivery.SubscriberModule == "fulfillment"
                && delivery.SubscriberIdentity == "fulfillment.sales-order-projection.v1");
        Assert.Contains(
            eventPipeline.Deliveries,
            delivery =>
                delivery.SubscriberModule == "billing"
                && delivery.SubscriberIdentity == "billing.sales-order-projection.v1");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DescribeReceiveQueue_WhenQueueIsBound_ReturnsBindingDiagnostic()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());
        services.AddBondstone(
            bondstone => bondstone.UseRabbitMqTransport(
                rabbitMq =>
                    rabbitMq.ReceiveQueue("sales-events")
                        .SubscribeEvent(
                            "sales.order.submitted.v1",
                            "fulfillment",
                            "fulfillment.sales-order-projection.v1")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IRabbitMqTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IRabbitMqTopologyDiagnostics>();

        RabbitMqReceiveQueueDiagnostic diagnostic =
            diagnostics.DescribeReceiveQueue("sales-events");

        Assert.True(diagnostic.HasBinding);
        Assert.Empty(diagnostic.AcceptedModules);
        Assert.Single(diagnostic.EventSubscriptions);
        Assert.Equal(
            "sales.order.submitted.v1",
            diagnostic.EventSubscriptions.Single().MessageTypeName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenQueueIsNotBound_Throws()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline(),
            _ => { });
        IRabbitMqReceivedMessageDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRabbitMqReceivedMessageDispatcher>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync("fulfillment.commands", CreateMessage()));

        Assert.Contains("is not bound", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FromBasicDeliver_WhenPropertiesContainIdentity_ReturnsTransportMessage()
    {
        RabbitMqTransportMessage source = CreateMessage();
        var properties = new BasicProperties
        {
            MessageId = source.MessageId,
            Type = source.MessageTypeName,
            CorrelationId = source.CorrelationId,
            Headers = source.Headers.ToDictionary(
                static entry => entry.Key,
                static entry => (object?)entry.Value,
                StringComparer.Ordinal),
        };
        var delivery = new BasicDeliverEventArgs(
            "consumer-1",
            deliveryTag: 1,
            redelivered: false,
            exchange: "bondstone.commands",
            routingKey: "fulfillment.commands",
            properties,
            Encoding.UTF8.GetBytes(source.Body),
            CancellationToken.None);

        RabbitMqTransportMessage mapped =
            RabbitMqReceivedMessageMapper.FromBasicDeliver(delivery);

        Assert.Equal(source.Body, mapped.Body);
        Assert.Equal(source.MessageId, mapped.MessageId);
        Assert.Equal(source.MessageTypeName, mapped.MessageTypeName);
        Assert.Equal(source.CorrelationId, mapped.CorrelationId);
        Assert.Equal(
            MessageKind.Command.ToString(),
            mapped.Headers[BondstoneRabbitMqHeaders.MessageKind]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FromBodyAndProperties_WhenIdentityIsOnlyInHeaders_ReturnsTransportMessage()
    {
        RabbitMqTransportMessage source = CreateMessage();
        var properties = new BasicProperties
        {
            Headers = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                [BondstoneRabbitMqHeaders.MessageId] = Encoding.UTF8.GetBytes(source.MessageId),
                [BondstoneRabbitMqHeaders.MessageTypeName] = source.MessageTypeName,
            },
        };

        RabbitMqTransportMessage mapped =
            RabbitMqReceivedMessageMapper.FromBodyAndProperties(
                Encoding.UTF8.GetBytes(source.Body),
                properties);

        Assert.Equal(source.MessageId, mapped.MessageId);
        Assert.Equal(source.MessageTypeName, mapped.MessageTypeName);
        Assert.Equal(source.MessageId, mapped.CorrelationId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WhenDispatchSucceeds_AcknowledgesDelivery()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline(),
            rabbitMq =>
                rabbitMq.ReceiveQueue("fulfillment.commands")
                    .AcceptModule("fulfillment"));
        IRabbitMqReceivedMessageHandler handler =
            serviceProvider.GetRequiredService<IRabbitMqReceivedMessageHandler>();
        BasicDeliverEventArgs delivery = CreateDelivery(CreateMessage());
        ulong? acknowledgedDeliveryTag = null;

        await handler.HandleAsync(
            "fulfillment.commands",
            delivery,
            (deliveryTag, _) =>
            {
                acknowledgedDeliveryTag = deliveryTag;
                return ValueTask.CompletedTask;
            });

        Assert.Equal(delivery.DeliveryTag, acknowledgedDeliveryTag);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WhenDispatchFails_DoesNotAcknowledgeDelivery()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline(),
            _ => { });
        IRabbitMqReceivedMessageHandler handler =
            serviceProvider.GetRequiredService<IRabbitMqReceivedMessageHandler>();
        bool acknowledged = false;

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.HandleAsync(
                "fulfillment.commands",
                CreateDelivery(CreateMessage()),
                (_, _) =>
                {
                    acknowledged = true;
                    return ValueTask.CompletedTask;
                }));

        Assert.False(acknowledged);
    }

    private static ServiceProvider CreateServiceProvider(
        RecordingCommandReceivePipeline commandPipeline,
        RecordingEventReceivePipeline eventPipeline,
        Action<BondstoneRabbitMqTransportBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRabbitMqMessagePublisher>(new RecordingRabbitMqMessagePublisher());
        services.AddSingleton<IModuleCommandReceivePipeline>(commandPipeline);
        services.AddSingleton<IModuleEventReceivePipeline>(eventPipeline);
        services.AddBondstone(bondstone => bondstone.UseRabbitMqTransport(configure));

        return services.BuildServiceProvider();
    }

    private static RabbitMqTransportMessage CreateMessage(
        MessageKind messageKind = MessageKind.Command,
        string? targetModule = "fulfillment",
        string messageTypeName = "fulfillment.order.reserve.v1")
    {
        var envelope = new RabbitMqDurableMessageEnvelope(
            Guid.Parse("3f1a9e26-75d4-4a7d-bb48-ae453f5e5e02"),
            messageKind.ToString(),
            messageTypeName,
            "sales",
            targetModule,
            """{"orderId":"A-100"}""",
            null,
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"),
            Guid.Parse("5dac5be5-d1ef-432d-a5d5-597103ae44c9"),
            null,
            null,
            null,
            Guid.Parse("a2d07b16-258d-4ad2-b310-1ef95d5c0936"),
            "orders/A-100");
        string messageId = envelope.MessageId.ToString("D");

        return new RabbitMqTransportMessage(
            JsonSerializer.Serialize(envelope),
            messageId,
            messageTypeName,
            envelope.DurableOperationId!.Value.ToString("D"),
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [BondstoneRabbitMqHeaders.MessageId] = messageId,
                [BondstoneRabbitMqHeaders.MessageKind] = messageKind.ToString(),
                [BondstoneRabbitMqHeaders.MessageTypeName] = messageTypeName,
            });
    }

    private static BasicDeliverEventArgs CreateDelivery(
        RabbitMqTransportMessage message)
    {
        var properties = new BasicProperties
        {
            MessageId = message.MessageId,
            Type = message.MessageTypeName,
            CorrelationId = message.CorrelationId,
            Headers = message.Headers.ToDictionary(
                static entry => entry.Key,
                static entry => (object?)entry.Value,
                StringComparer.Ordinal),
        };

        return new BasicDeliverEventArgs(
            "consumer-1",
            deliveryTag: 42,
            redelivered: false,
            exchange: "bondstone.commands",
            routingKey: "fulfillment.commands",
            properties,
            Encoding.UTF8.GetBytes(message.Body),
            CancellationToken.None);
    }

    private static DurableInboxHandleResult CreateHandledResult(
        DurableMessageEnvelope envelope,
        string moduleName,
        string handlerIdentity)
    {
        return new DurableInboxHandleResult(
            DurableInboxHandleStatus.Handled,
            new DurableInboxRecord(
                new DurableInboxMessageKey(
                    envelope.MessageId,
                    moduleName,
                    handlerIdentity),
                DateTimeOffset.Parse("2026-06-09T12:00:02+00:00"),
                DateTimeOffset.Parse("2026-06-09T12:00:03+00:00")));
    }

    private sealed class RecordingRabbitMqMessagePublisher : IRabbitMqMessagePublisher
    {
        public ValueTask PublishAsync(
            RabbitMqPublishDestination destination,
            RabbitMqTransportMessage message,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingCommandReceivePipeline : IModuleCommandReceivePipeline
    {
        public DurableMessageEnvelope? Envelope { get; private set; }

        public int HandledCount { get; private set; }

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            Envelope = envelope;
            HandledCount++;

            return ValueTask.FromResult(
                CreateHandledResult(
                    envelope,
                    envelope.TargetModule!,
                    envelope.MessageTypeName));
        }
    }

    private sealed class RecordingEventReceivePipeline : IModuleEventReceivePipeline
    {
        public List<EventDelivery> Deliveries { get; } = [];

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            Deliveries.Add(new EventDelivery(
                envelope,
                subscriberModule,
                subscriberIdentity));

            return ValueTask.FromResult(
                CreateHandledResult(
                    envelope,
                    subscriberModule,
                    subscriberIdentity));
        }
    }

    private sealed record EventDelivery(
        DurableMessageEnvelope Envelope,
        string SubscriberModule,
        string SubscriberIdentity);
}
