using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.ServiceBus.Inbox;
using Bondstone.Transport.ServiceBus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bondstone.Transport.ServiceBus.Tests;

public sealed class ServiceBusReceiveWorkerIntegrationTests(ServiceBusFixture fixture)
    : IClassFixture<ServiceBusFixture>
{
    private const string QueueName = "queue.1";
    private const string TopicName = "topic.1";
    private const string SubscriptionName = "subscription.3";

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_WhenCommandDeliveryHandled_CompletesBrokerMessage()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        CancellationToken ct = timeout.Token;
        var commandPipeline = new SignalingCommandReceivePipeline();
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        await DrainQueueAsync(client, QueueName, ct);
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            client,
            commandPipeline,
            new RecordingEventReceivePipeline(),
            serviceBus =>
            {
                serviceBus.ReceiveQueue(QueueName)
                    .AcceptModule("fulfillment");
                serviceBus.UseReceiveWorker();
            });
        IHostedService receiveWorker = GetReceiveWorker(serviceProvider);

        await receiveWorker.StartAsync(ct);
        try
        {
            await SendAsync(client, QueueName, CreateMessage(), ct);

            DurableMessageEnvelope envelope = await commandPipeline.WaitUntilHandledAsync(ct);

            Assert.Equal("fulfillment", envelope.TargetModule);
            Assert.Equal("fulfillment.order.reserve.v1", envelope.MessageTypeName);
            await AssertQueueIsEmptyAsync(client, QueueName, ct);
        }
        finally
        {
            await receiveWorker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_WhenEventSubscriptionHasSubscribers_DispatchesEachSubscriber()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        CancellationToken ct = timeout.Token;
        var eventPipeline = new SignalingEventReceivePipeline(expectedDeliveryCount: 2);
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        await DrainSubscriptionAsync(client, TopicName, SubscriptionName, ct);
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            client,
            new SignalingCommandReceivePipeline(),
            eventPipeline,
            serviceBus =>
            {
                serviceBus.ReceiveSubscription(TopicName, SubscriptionName)
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "fulfillment",
                        "fulfillment.sales-order-projection.v1")
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "billing",
                        "billing.sales-order-projection.v1");
                serviceBus.UseReceiveWorker();
            });
        IHostedService receiveWorker = GetReceiveWorker(serviceProvider);

        await receiveWorker.StartAsync(ct);
        try
        {
            await SendAsync(client, TopicName, CreateEventMessage(), ct);

            IReadOnlyCollection<EventDelivery> deliveries =
                await eventPipeline.WaitUntilHandledAsync(ct);

            Assert.Contains(
                deliveries,
                delivery =>
                    delivery.SubscriberModule == "fulfillment"
                    && delivery.SubscriberIdentity == "fulfillment.sales-order-projection.v1");
            Assert.Contains(
                deliveries,
                delivery =>
                    delivery.SubscriberModule == "billing"
                    && delivery.SubscriberIdentity == "billing.sales-order-projection.v1");
        }
        finally
        {
            await receiveWorker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_WhenDispatchFails_LeavesDeadLetterHandoffToServiceBus()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        CancellationToken ct = timeout.Token;
        ServiceBusTransportMessage message = CreateMessage();
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        await DrainQueueAsync(client, QueueName, ct);
        await DrainDeadLetterQueueAsync(client, QueueName, ct);
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            client,
            new SignalingCommandReceivePipeline(),
            new RecordingEventReceivePipeline(),
            serviceBus =>
            {
                serviceBus.ReceiveQueue(QueueName);
                serviceBus.UseReceiveWorker();
            });
        IHostedService receiveWorker = GetReceiveWorker(serviceProvider);

        await receiveWorker.StartAsync(ct);
        try
        {
            await SendAsync(client, QueueName, message, ct);

            ServiceBusTransportMessage deadLettered =
                await WaitForDeadLetterMessageAsync(client, QueueName, ct);

            Assert.Equal(message.MessageId, deadLettered.MessageId);
            Assert.Equal(message.Subject, deadLettered.Subject);
        }
        finally
        {
            await receiveWorker.StopAsync(CancellationToken.None);
        }
    }

    private static async Task SendAsync(
        ServiceBusClient client,
        string entityName,
        ServiceBusTransportMessage message,
        CancellationToken ct)
    {
        await using ServiceBusSender sender = client.CreateSender(entityName);
        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromString(message.Body))
        {
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            Subject = message.Subject,
            PartitionKey = message.PartitionKey,
        };

        foreach (KeyValuePair<string, object> property in message.ApplicationProperties)
        {
            serviceBusMessage.ApplicationProperties[property.Key] = property.Value;
        }

        await sender.SendMessageAsync(serviceBusMessage, ct);
    }

    private static async Task DrainQueueAsync(
        ServiceBusClient client,
        string queueName,
        CancellationToken ct)
    {
        await using ServiceBusReceiver receiver = client.CreateReceiver(queueName);
        await DrainReceiverAsync(receiver, ct);
    }

    private static async Task DrainDeadLetterQueueAsync(
        ServiceBusClient client,
        string queueName,
        CancellationToken ct)
    {
        await using ServiceBusReceiver receiver = client.CreateReceiver(
            queueName,
            new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter,
            });
        await DrainReceiverAsync(receiver, ct);
    }

    private static async Task DrainSubscriptionAsync(
        ServiceBusClient client,
        string topicName,
        string subscriptionName,
        CancellationToken ct)
    {
        await using ServiceBusReceiver receiver = client.CreateReceiver(
            topicName,
            subscriptionName);
        await DrainReceiverAsync(receiver, ct);
    }

    private static async Task DrainReceiverAsync(
        ServiceBusReceiver receiver,
        CancellationToken ct)
    {
        while (true)
        {
            ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync(
                TimeSpan.FromMilliseconds(100),
                ct);
            if (message is null)
            {
                return;
            }

            await receiver.CompleteMessageAsync(message, ct);
        }
    }

    private static async Task AssertQueueIsEmptyAsync(
        ServiceBusClient client,
        string queueName,
        CancellationToken ct)
    {
        await using ServiceBusReceiver receiver = client.CreateReceiver(queueName);
        ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync(
            TimeSpan.FromMilliseconds(500),
            ct);

        Assert.Null(message);
    }

    private static async Task<ServiceBusTransportMessage> WaitForDeadLetterMessageAsync(
        ServiceBusClient client,
        string queueName,
        CancellationToken ct)
    {
        await using ServiceBusReceiver receiver = client.CreateReceiver(
            queueName,
            new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter,
            });

        while (true)
        {
            ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync(
                TimeSpan.FromMilliseconds(250),
                ct);
            if (message is not null)
            {
                ServiceBusTransportMessage transportMessage =
                    ServiceBusReceivedMessageMapper.FromReceivedMessage(message);
                await receiver.CompleteMessageAsync(message, ct);
                return transportMessage;
            }
        }
    }

    private static ServiceProvider CreateServiceProvider(
        ServiceBusClient client,
        SignalingCommandReceivePipeline commandPipeline,
        IModuleEventReceivePipeline eventPipeline,
        Action<BondstoneServiceBusTransportBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<IModuleCommandReceivePipeline>(commandPipeline);
        services.AddSingleton(eventPipeline);
        services.AddBondstoneServiceBusClient(client);
        services.AddBondstone(bondstone => bondstone.Outbox.UseServiceBusTransport(configure));

        return services.BuildServiceProvider();
    }

    private static IHostedService GetReceiveWorker(
        ServiceProvider serviceProvider)
    {
        return serviceProvider
            .GetServices<IHostedService>()
            .Single(service => service.GetType().Name == "ServiceBusReceiveWorker");
    }

    private static ServiceBusTransportMessage CreateMessage()
    {
        var envelope = new ServiceBusDurableMessageEnvelope(
            Guid.NewGuid(),
            MessageKind.Command.ToString(),
            "fulfillment.order.reserve.v1",
            "sales",
            "fulfillment",
            """{"orderId":"A-100"}""",
            null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            null,
            null,
            null,
            Guid.NewGuid(),
            "orders/A-100");
        string messageId = envelope.MessageId.ToString("D");

        return new ServiceBusTransportMessage(
            JsonSerializer.Serialize(envelope),
            messageId,
            envelope.MessageTypeName,
            envelope.DurableOperationId!.Value.ToString("D"),
            envelope.PartitionKey,
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [BondstoneServiceBusHeaders.MessageId] = messageId,
                [BondstoneServiceBusHeaders.MessageKind] = MessageKind.Command.ToString(),
                [BondstoneServiceBusHeaders.MessageTypeName] = envelope.MessageTypeName,
            });
    }

    private static ServiceBusTransportMessage CreateEventMessage()
    {
        var envelope = new ServiceBusDurableMessageEnvelope(
            Guid.NewGuid(),
            MessageKind.Event.ToString(),
            "sales.order.submitted.v1",
            "sales",
            null,
            """{"orderId":"A-100"}""",
            null,
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            null,
            null,
            null,
            Guid.NewGuid(),
            "orders/A-100");
        string messageId = envelope.MessageId.ToString("D");

        return new ServiceBusTransportMessage(
            JsonSerializer.Serialize(envelope),
            messageId,
            envelope.MessageTypeName,
            envelope.DurableOperationId!.Value.ToString("D"),
            envelope.PartitionKey,
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [BondstoneServiceBusHeaders.MessageId] = messageId,
                [BondstoneServiceBusHeaders.MessageKind] = MessageKind.Event.ToString(),
                [BondstoneServiceBusHeaders.MessageTypeName] = envelope.MessageTypeName,
            });
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
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow));
    }

    private sealed class SignalingCommandReceivePipeline : IModuleCommandReceivePipeline
    {
        private readonly TaskCompletionSource<DurableMessageEnvelope> _handled =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            _handled.TrySetResult(envelope);

            return ValueTask.FromResult(
                CreateHandledResult(
                    envelope,
                    envelope.TargetModule!,
                    envelope.MessageTypeName));
        }

        public async Task<DurableMessageEnvelope> WaitUntilHandledAsync(
            CancellationToken ct)
        {
            await using (ct.Register(
                static state =>
                    ((TaskCompletionSource<DurableMessageEnvelope>)state!)
                        .TrySetCanceled(),
                _handled))
            {
                return await _handled.Task;
            }
        }
    }

    private sealed class RecordingEventReceivePipeline : IModuleEventReceivePipeline
    {
        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException("The Service Bus integration test does not dispatch events.");
        }
    }

    private sealed class SignalingEventReceivePipeline(int expectedDeliveryCount)
        : IModuleEventReceivePipeline
    {
        private readonly List<EventDelivery> _deliveries = [];
        private readonly Lock _lock = new();
        private readonly TaskCompletionSource<IReadOnlyCollection<EventDelivery>> _handled =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            lock (_lock)
            {
                _deliveries.Add(new EventDelivery(
                    envelope,
                    subscriberModule,
                    subscriberIdentity));

                if (_deliveries.Count == expectedDeliveryCount)
                {
                    _handled.TrySetResult(_deliveries.ToArray());
                }
            }

            return ValueTask.FromResult(
                CreateHandledResult(
                    envelope,
                    subscriberModule,
                    subscriberIdentity));
        }

        public async Task<IReadOnlyCollection<EventDelivery>> WaitUntilHandledAsync(
            CancellationToken ct)
        {
            await using (ct.Register(
                static state =>
                    ((TaskCompletionSource<IReadOnlyCollection<EventDelivery>>)state!)
                        .TrySetCanceled(),
                _handled))
            {
                return await _handled.Task;
            }
        }
    }

    private sealed record EventDelivery(
        DurableMessageEnvelope Envelope,
        string SubscriberModule,
        string SubscriberIdentity);
}
