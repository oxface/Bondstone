using Azure.Messaging.ServiceBus;
using Bondstone.Configuration;
using Bondstone.Messaging;
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
    public async Task ReceiveWorker_ConsumesServiceBusEnvelopeAndCallsBondstoneReceiver()
    {
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        var receiver = new CapturingEnvelopeReceiver();
        ServiceProvider provider = BuildReceiveProvider(client, receiver);
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

                CapturedEnvelope captured = await receiver.WaitAsync(
                    CancellationToken.None);
                Assert.Equal(envelope.MessageId, captured.Envelope.MessageId);
                Assert.Null(captured.Binding);
            }
            finally
            {
                await worker.StopAsync(CancellationToken.None);
            }
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_ConsumesServiceBusEventEnvelopeWithBinding()
    {
        await using var client = new ServiceBusClient(fixture.ConnectionString);
        var receiver = new CapturingEnvelopeReceiver();
        ServiceProvider provider = BuildReceiveProvider(client, receiver, receiveEvent: true);
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

                CapturedEnvelope captured = await receiver.WaitAsync(
                    CancellationToken.None);
                Assert.Equal(envelope.MessageId, captured.Envelope.MessageId);
                Assert.Equal(
                    new DurableEnvelopeReceiveBinding(
                        SubscriberModule,
                        SubscriberIdentity),
                    captured.Binding);
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
        CapturingEnvelopeReceiver receiver,
        bool receiveEvent = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(client);
        services.AddLogging();
        services.AddSingleton<IDurableEnvelopeReceiver>(receiver);
        services.AddBondstone(bondstone =>
        {
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

    private sealed class CapturingEnvelopeReceiver : IDurableEnvelopeReceiver
    {
        private readonly TaskCompletionSource<CapturedEnvelope> _received =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            DurableMessageEnvelope envelope,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            _received.TrySetResult(new CapturedEnvelope(envelope, binding));
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DurableInboxMessageKey key = binding is null
                ? DurableInboxMessageKey.ForCommandHandler(
                    envelope.MessageId,
                    envelope.TargetModule ?? "test",
                    envelope.MessageTypeName)
                : DurableInboxMessageKey.ForEventSubscriber(
                    envelope.MessageId,
                    binding.SubscriberModule,
                    binding.SubscriberIdentity);
            return new ValueTask<DurableInboxHandleResult>(
                new DurableInboxHandleResult(
                    DurableInboxHandleStatus.Handled,
                    new DurableInboxRecord(
                        key,
                        now,
                        now)));
        }

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            ReadOnlyMemory<byte> utf8Json,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            var serializer = new SystemTextJsonDurableMessageEnvelopeSerializer();
            return ReceiveAsync(serializer.Deserialize(utf8Json), binding, ct);
        }

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            string json,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            var serializer = new SystemTextJsonDurableMessageEnvelopeSerializer();
            return ReceiveAsync(serializer.Deserialize(json), binding, ct);
        }

        public ValueTask<DurableInboxHandleResult> ReceiveCommandAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            return ReceiveAsync(envelope, binding: null, ct);
        }

        public ValueTask<DurableInboxHandleResult> ReceiveEventAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            return ReceiveAsync(
                envelope,
                new DurableEnvelopeReceiveBinding(
                    subscriberModule,
                    subscriberIdentity),
                ct);
        }

        public async Task<CapturedEnvelope> WaitAsync(
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
                throw new TimeoutException("Service Bus receive worker did not call Bondstone receiver.");
            }

            return await _received.Task;
        }
    }

    private sealed record CapturedEnvelope(
        DurableMessageEnvelope Envelope,
        DurableEnvelopeReceiveBinding? Binding);
}
