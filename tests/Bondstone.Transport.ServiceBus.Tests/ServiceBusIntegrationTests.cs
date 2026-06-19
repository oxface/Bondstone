using Azure.Messaging.ServiceBus;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bondstone.Transport.ServiceBus.Tests;

public sealed class ServiceBusIntegrationTests(ServiceBusFixture fixture)
    : IClassFixture<ServiceBusFixture>
{
    private const string QueueName = "queue.1";
    private const string TopicName = "topic.1";
    private const string SubscriptionName = "subscription.3";
    private const string SubscriberModule = "billing";
    private const string SubscriberIdentity = "billing.order-placed-projection.v1";

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Dispatcher_PublishesDurableEnvelopeToServiceBusQueue()
    {
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        ServiceProvider provider = BuildDispatcherProvider(client);
        await using (provider.ConfigureAwait(false))
        {
            IDurableEnvelopeDispatcher dispatcher =
                provider.GetRequiredService<IDurableEnvelopeDispatcher>();
            DurableMessageEnvelope envelope = CreateCommandEnvelope();

            await dispatcher.DispatchAsync(
                new DurableOutboxRecord(envelope, DateTimeOffset.UtcNow),
                CancellationToken.None);

            await using ServiceBusReceiver receiver =
                client.CreateReceiver(QueueName);
            ServiceBusReceivedMessage message = await WaitForMessageAsync(
                receiver,
                CancellationToken.None);

            Assert.Equal(envelope.MessageId.ToString("D"), message.MessageId);
            Assert.Equal(envelope.MessageTypeName, message.Subject);

            IDurableMessageEnvelopeSerializer serializer =
                provider.GetRequiredService<IDurableMessageEnvelopeSerializer>();
            DurableMessageEnvelope received =
                serializer.Deserialize(message.Body.ToMemory());
            Assert.Equal(envelope.MessageId, received.MessageId);
            Assert.Equal(envelope.Payload, received.Payload);

            await receiver.CompleteMessageAsync(
                message,
                CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Dispatcher_PublishesDurableEventToServiceBusTopic()
    {
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        ServiceProvider provider = BuildDispatcherProvider(client, TopicName);
        await using (provider.ConfigureAwait(false))
        {
            IDurableEnvelopeDispatcher dispatcher =
                provider.GetRequiredService<IDurableEnvelopeDispatcher>();
            DurableMessageEnvelope envelope = CreateEventEnvelope();

            await dispatcher.DispatchAsync(
                new DurableOutboxRecord(envelope, DateTimeOffset.UtcNow),
                CancellationToken.None);

            await using ServiceBusReceiver receiver =
                client.CreateReceiver(TopicName, SubscriptionName);
            ServiceBusReceivedMessage message = await WaitForMessageAsync(
                receiver,
                CancellationToken.None);

            Assert.Equal(envelope.MessageId.ToString("D"), message.MessageId);
            Assert.Equal(envelope.MessageTypeName, message.Subject);

            IDurableMessageEnvelopeSerializer serializer =
                provider.GetRequiredService<IDurableMessageEnvelopeSerializer>();
            DurableMessageEnvelope received =
                serializer.Deserialize(message.Body.ToMemory());
            Assert.Equal(MessageKind.Event, received.MessageKind);
            Assert.Equal(envelope.MessageId, received.MessageId);

            await receiver.CompleteMessageAsync(
                message,
                CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_ConsumesServiceBusEnvelopeAndIngestsDurableIncomingInboxRecord()
    {
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        var ingestionStore = new CapturingIncomingInboxIngestionStore();
        ServiceProvider provider = BuildReceiveProvider(client, ingestionStore);
        await using (provider.ConfigureAwait(false))
        {
            IHostedService worker = provider
                .GetServices<IHostedService>()
                .Single();
            await worker.StartAsync(CancellationToken.None);
            try
            {
                DurableMessageEnvelope envelope = CreateCommandEnvelope();
                IDurableMessageEnvelopeSerializer serializer =
                    provider.GetRequiredService<IDurableMessageEnvelopeSerializer>();

                await using ServiceBusSender sender =
                    client.CreateSender(QueueName);
                await sender.SendMessageAsync(
                    new ServiceBusMessage(BinaryData.FromBytes(
                        serializer.SerializeToUtf8Bytes(envelope)))
                    {
                        MessageId = envelope.MessageId.ToString("D"),
                        Subject = envelope.MessageTypeName,
                    },
                    CancellationToken.None);

                DurableIncomingInboxRecord captured = await ingestionStore.WaitAsync(
                    CancellationToken.None);
                Assert.Equal(envelope.MessageId, captured.Envelope.MessageId);
                Assert.Equal("fulfillment", captured.ReceiverModule);
                Assert.Equal("fulfillment.reserve-inventory.v1", captured.HandlerIdentity);
                Assert.Equal($"servicebus:{QueueName}", captured.SourceTransportName);
            }
            finally
            {
                await worker.StopAsync(CancellationToken.None);
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_ConsumesServiceBusEventEnvelopeWithBindingAndIngestsDurableIncomingInboxRecord()
    {
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        var ingestionStore = new CapturingIncomingInboxIngestionStore();
        ServiceProvider provider = BuildReceiveProvider(client, ingestionStore, receiveEvent: true);
        await using (provider.ConfigureAwait(false))
        {
            IHostedService worker = provider
                .GetServices<IHostedService>()
                .Single();
            await worker.StartAsync(CancellationToken.None);
            try
            {
                DurableMessageEnvelope envelope = CreateEventEnvelope();
                IDurableMessageEnvelopeSerializer serializer =
                    provider.GetRequiredService<IDurableMessageEnvelopeSerializer>();

                await using ServiceBusSender sender =
                    client.CreateSender(TopicName);
                await sender.SendMessageAsync(
                    new ServiceBusMessage(BinaryData.FromBytes(
                        serializer.SerializeToUtf8Bytes(envelope)))
                    {
                        MessageId = envelope.MessageId.ToString("D"),
                        Subject = envelope.MessageTypeName,
                    },
                    CancellationToken.None);

                DurableIncomingInboxRecord captured = await ingestionStore.WaitAsync(
                    CancellationToken.None);
                Assert.Equal(envelope.MessageId, captured.Envelope.MessageId);
                Assert.Equal(SubscriberModule, captured.ReceiverModule);
                Assert.Equal(SubscriberIdentity, captured.HandlerIdentity);
                Assert.Equal($"servicebus:{TopicName}/{SubscriptionName}", captured.SourceTransportName);
            }
            finally
            {
                await worker.StopAsync(CancellationToken.None);
            }
        }
    }

    private static ServiceProvider BuildDispatcherProvider(
        ServiceBusClient client)
    {
        return BuildDispatcherProvider(client, QueueName);
    }

    private static ServiceProvider BuildDispatcherProvider(
        ServiceBusClient client,
        string entityName)
    {
        var services = new ServiceCollection();
        services.AddSingleton(client);
        services.AddBondstone(bondstone =>
        {
            bondstone.Outbox.MarkPersistenceProvider("test");
            bondstone.UseServiceBusDispatcher(options =>
            {
                options.ResolveEntityName = _ => entityName;
            });
            bondstone.Outbox.MarkDispatcher("test");
        });
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static ServiceProvider BuildReceiveProvider(
        ServiceBusClient client,
        CapturingIncomingInboxIngestionStore ingestionStore,
        bool receiveEvent = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(client);
        services.AddLogging();
        services.AddSingleton<IDurableIncomingInboxIngestionStore>(ingestionStore);
        services.AddSingleton<IDurableIncomingInboxIngestionPersistenceScope>(
            new CapturingIncomingInboxIngestionScope());
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
            });
            bondstone.Module(SubscriberModule, module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Events.RegisterSubscriber<OrderPlacedEvent, OrderPlacedHandler>(
                    SubscriberIdentity);
            });
            bondstone.UseServiceBusReceiveWorker(options =>
            {
                if (receiveEvent)
                {
                    options.TopicName = TopicName;
                    options.SubscriptionName = SubscriptionName;
                    options.ReceiveEvent(
                        SubscriberModule,
                        SubscriberIdentity);
                }
                else
                {
                    options.QueueName = QueueName;
                    options.ReceiveCommand();
                }
            });
        });
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<ServiceBusReceivedMessage> WaitForMessageAsync(
        ServiceBusReceiver receiver,
        CancellationToken ct)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

        while (!linked.IsCancellationRequested)
        {
            ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync(
                TimeSpan.FromMilliseconds(500),
                linked.Token);
            if (message is not null)
            {
                return message;
            }
        }

        throw new TimeoutException("Service Bus queue did not receive a durable envelope.");
    }

    private static DurableMessageEnvelope CreateCommandEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.NewGuid(),
            MessageKind.Command,
            "fulfillment.reserve-inventory.v1",
            "ordering",
            "fulfillment",
            """{"orderId":"O-100"}""",
            DateTimeOffset.UtcNow);
    }

    private static DurableMessageEnvelope CreateEventEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.NewGuid(),
            MessageKind.Event,
            "ordering.order-placed.v1",
            "ordering",
            targetModule: null,
            """{"orderId":"O-100"}""",
            DateTimeOffset.UtcNow);
    }

    private sealed class CapturingIncomingInboxIngestionStore : IDurableIncomingInboxIngestionStore
    {
        private readonly TaskCompletionSource<DurableIncomingInboxRecord> _received =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<DurableIncomingInboxIngestionResult> IngestAsync(
            DurableIncomingInboxRecord record,
            CancellationToken ct = default)
        {
            _received.TrySetResult(record);
            return ValueTask.FromResult(
                new DurableIncomingInboxIngestionResult(
                    DurableIncomingInboxIngestionStatus.Ingested,
                    record));
        }

        public async Task<DurableIncomingInboxRecord> WaitAsync(
            CancellationToken ct)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

            Task completed = await Task.WhenAny(
                _received.Task,
                Task.Delay(Timeout.InfiniteTimeSpan, linked.Token));

            if (completed != _received.Task)
            {
                throw new TimeoutException("Service Bus receive worker did not ingest a durable incoming inbox record.");
            }

            return await _received.Task;
        }
    }

    private sealed class CapturingIncomingInboxIngestionScope
        : IDurableIncomingInboxIngestionPersistenceScope
    {
        public async ValueTask<TResult> ExecuteAsync<TResult>(
            Func<IDurableIncomingInboxIngestionPersistenceScope, CancellationToken, ValueTask<TResult>> operation,
            CancellationToken ct = default)
        {
            return await operation(this, ct);
        }

        public ValueTask SaveChangesAsync(
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("fulfillment.reserve-inventory.v1")]
    private sealed record ReserveInventoryCommand(string OrderId) : IDurableCommand;

    private sealed class ReserveInventoryHandler : ICommandHandler<ReserveInventoryCommand>
    {
        public ValueTask HandleAsync(
            ReserveInventoryCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [IntegrationEventIdentity("ordering.order-placed.v1")]
    private sealed record OrderPlacedEvent(string OrderId) : IIntegrationEvent;

    private sealed class OrderPlacedHandler : IIntegrationEventHandler<OrderPlacedEvent>
    {
        public ValueTask HandleAsync(
            OrderPlacedEvent integrationEvent,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
