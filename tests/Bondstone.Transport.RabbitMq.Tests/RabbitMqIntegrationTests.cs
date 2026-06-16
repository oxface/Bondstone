using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Xunit;

namespace Bondstone.Transport.RabbitMq.Tests;

public sealed class RabbitMqIntegrationTests(RabbitMqFixture fixture)
    : IClassFixture<RabbitMqFixture>
{
    private const string SubscriberModule = "billing";
    private const string SubscriberIdentity = "billing.order-placed-projection.v1";

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Dispatcher_PublishesDurableEnvelopeToRabbitMqQueue()
    {
        await using RabbitMqConnectionContext context =
            await RabbitMqConnectionContext.OpenAsync(fixture.ConnectionString);
        string queueName = NewQueueName();
        await context.Channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: CancellationToken.None);

        ServiceProvider provider = BuildDispatcherProvider(
            context.Channel,
            queueName);
        await using (provider.ConfigureAwait(false))
        {
            IDurableEnvelopeDispatcher dispatcher =
                provider.GetRequiredService<IDurableEnvelopeDispatcher>();
            DurableMessageEnvelope envelope = CreateCommandEnvelope();

            await dispatcher.DispatchAsync(
                new DurableOutboxRecord(envelope, DateTimeOffset.UtcNow),
                CancellationToken.None);

            BasicGetResult? result = await WaitForMessageAsync(
                context.Channel,
                queueName,
                CancellationToken.None);

            Assert.NotNull(result);
            IDurableMessageEnvelopeSerializer serializer =
                provider.GetRequiredService<IDurableMessageEnvelopeSerializer>();
            DurableMessageEnvelope received =
                serializer.Deserialize(result.Body);
            Assert.Equal(envelope.MessageId, received.MessageId);
            Assert.Equal(envelope.Payload, received.Payload);

            await context.Channel.BasicAckAsync(
                result.DeliveryTag,
                multiple: false,
                cancellationToken: CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Dispatcher_PublishesDurableEventToRabbitMqExchangeBoundQueue()
    {
        await using RabbitMqConnectionContext context =
            await RabbitMqConnectionContext.OpenAsync(fixture.ConnectionString);
        string exchangeName = NewExchangeName();
        string queueName = NewQueueName();
        const string routingKey = "ordering.order-placed.v1";
        await context.Channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: CancellationToken.None);
        await context.Channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: CancellationToken.None);
        await context.Channel.QueueBindAsync(
            queueName,
            exchangeName,
            routingKey,
            cancellationToken: CancellationToken.None);

        ServiceProvider provider = BuildDispatcherProvider(
            context.Channel,
            new RabbitMqEnvelopeDestination(exchangeName, routingKey));
        await using (provider.ConfigureAwait(false))
        {
            IDurableEnvelopeDispatcher dispatcher =
                provider.GetRequiredService<IDurableEnvelopeDispatcher>();
            DurableMessageEnvelope envelope = CreateEventEnvelope();

            await dispatcher.DispatchAsync(
                new DurableOutboxRecord(envelope, DateTimeOffset.UtcNow),
                CancellationToken.None);

            BasicGetResult? result = await WaitForMessageAsync(
                context.Channel,
                queueName,
                CancellationToken.None);

            Assert.NotNull(result);
            IDurableMessageEnvelopeSerializer serializer =
                provider.GetRequiredService<IDurableMessageEnvelopeSerializer>();
            DurableMessageEnvelope received =
                serializer.Deserialize(result.Body);
            Assert.Equal(MessageKind.Event, received.MessageKind);
            Assert.Equal(envelope.MessageId, received.MessageId);

            await context.Channel.BasicAckAsync(
                result.DeliveryTag,
                multiple: false,
                cancellationToken: CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_ConsumesRabbitMqEnvelopeAndCallsBondstoneReceiver()
    {
        await using RabbitMqConnectionContext workerContext =
            await RabbitMqConnectionContext.OpenAsync(fixture.ConnectionString);
        string queueName = NewQueueName();
        await workerContext.Channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: CancellationToken.None);

        var receiver = new CapturingEnvelopeReceiver();
        ServiceProvider provider = BuildReceiveProvider(
            workerContext.Channel,
            queueName,
            receiver);
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
                byte[] body = serializer.SerializeToUtf8Bytes(envelope);

                await using RabbitMqConnectionContext publisherContext =
                    await RabbitMqConnectionContext.OpenAsync(fixture.ConnectionString);
                await publisherContext.Channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: queueName,
                    mandatory: true,
                    body: body,
                    cancellationToken: CancellationToken.None);

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
    public async Task ReceiveWorker_ConsumesRabbitMqEventEnvelopeWithBinding()
    {
        await using RabbitMqConnectionContext workerContext =
            await RabbitMqConnectionContext.OpenAsync(fixture.ConnectionString);
        string exchangeName = NewExchangeName();
        string queueName = NewQueueName();
        const string routingKey = "ordering.order-placed.v1";
        await workerContext.Channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: CancellationToken.None);
        await workerContext.Channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: CancellationToken.None);
        await workerContext.Channel.QueueBindAsync(
            queueName,
            exchangeName,
            routingKey,
            cancellationToken: CancellationToken.None);

        var receiver = new CapturingEnvelopeReceiver();
        ServiceProvider provider = BuildReceiveProvider(
            workerContext.Channel,
            queueName,
            receiver,
            receiveEvent: true);
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
                byte[] body = serializer.SerializeToUtf8Bytes(envelope);

                await using RabbitMqConnectionContext publisherContext =
                    await RabbitMqConnectionContext.OpenAsync(fixture.ConnectionString);
                await publisherContext.Channel.BasicPublishAsync(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    body: body,
                    cancellationToken: CancellationToken.None);

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
        IChannel channel,
        string queueName)
    {
        return BuildDispatcherProvider(
            channel,
            new RabbitMqEnvelopeDestination(string.Empty, queueName));
    }

    private static ServiceProvider BuildDispatcherProvider(
        IChannel channel,
        RabbitMqEnvelopeDestination destination)
    {
        var services = new ServiceCollection();
        services.AddSingleton(channel);
        services.AddBondstone(bondstone =>
        {
            bondstone.Outbox.MarkPersistenceProvider("test");
            bondstone.UseRabbitMqDispatcher(options =>
            {
                options.ResolveDestination = _ => destination;
            });
            bondstone.Outbox.MarkDispatcher("test");
        });
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static ServiceProvider BuildReceiveProvider(
        IChannel channel,
        string queueName,
        CapturingEnvelopeReceiver receiver,
        bool receiveEvent = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton(channel);
        services.AddLogging();
        services.AddSingleton<IDurableEnvelopeReceiver>(receiver);
        services.AddBondstone(bondstone =>
        {
            bondstone.UseRabbitMqReceiveWorker(options =>
            {
                options.QueueName = queueName;
                if (receiveEvent)
                {
                    options.ReceiveEvent(
                        SubscriberModule,
                        SubscriberIdentity);
                }
                else
                {
                    options.ReceiveCommand();
                }
            });
        });
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<BasicGetResult> WaitForMessageAsync(
        IChannel channel,
        string queueName,
        CancellationToken ct)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

        while (!linked.IsCancellationRequested)
        {
            BasicGetResult? result = await channel.BasicGetAsync(
                queueName,
                autoAck: false,
                cancellationToken: linked.Token);
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), linked.Token);
        }

        throw new TimeoutException("RabbitMQ queue did not receive a durable envelope.");
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

    private static string NewQueueName()
    {
        return $"bondstone.tests.{Guid.NewGuid():N}";
    }

    private static string NewExchangeName()
    {
        return $"bondstone.tests.{Guid.NewGuid():N}";
    }

    private sealed class RabbitMqConnectionContext : IAsyncDisposable
    {
        private readonly IConnection _connection;

        private RabbitMqConnectionContext(
            IConnection connection,
            IChannel channel)
        {
            _connection = connection;
            Channel = channel;
        }

        public IChannel Channel { get; }

        public static async Task<RabbitMqConnectionContext> OpenAsync(
            string connectionString)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
            };

            IConnection connection = await factory.CreateConnectionAsync(
                CancellationToken.None);
            IChannel channel = await connection.CreateChannelAsync(
                cancellationToken: CancellationToken.None);
            return new RabbitMqConnectionContext(connection, channel);
        }

        public async ValueTask DisposeAsync()
        {
            await Channel.DisposeAsync();
            await _connection.DisposeAsync();
        }
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
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token);

            Task completed = await Task.WhenAny(
                _received.Task,
                Task.Delay(Timeout.InfiniteTimeSpan, linked.Token));

            if (completed != _received.Task)
            {
                throw new TimeoutException("RabbitMQ receive worker did not call Bondstone receiver.");
            }

            return await _received.Task;
        }
    }

    private sealed record CapturedEnvelope(
        DurableMessageEnvelope Envelope,
        DurableEnvelopeReceiveBinding? Binding);
}
