using System.Text;
using System.Text.Json;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.RabbitMq.Inbox;
using Bondstone.Transport.RabbitMq.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Xunit;

namespace Bondstone.Transport.RabbitMq.Tests;

public sealed class RabbitMqReceiveWorkerIntegrationTests(RabbitMqFixture fixture)
    : IClassFixture<RabbitMqFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_WhenCommandDeliveryHandled_AcknowledgesBrokerMessage()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        CancellationToken ct = timeout.Token;
        string queueName = $"bondstone.tests.{Guid.NewGuid():N}";
        var commandPipeline = new SignalingCommandReceivePipeline();
        await using IConnection connection = await CreateConnectionAsync(ct);
        await DeclareQueueAsync(connection, queueName, ct);
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            connection,
            commandPipeline,
            queueName);
        IHostedService receiveWorker = serviceProvider
            .GetServices<IHostedService>()
            .Single(service => service.GetType().Name == "RabbitMqReceiveWorker");

        await receiveWorker.StartAsync(ct);
        try
        {
            await PublishCommandAsync(connection, queueName, CreateMessage(), ct);

            DurableMessageEnvelope envelope = await commandPipeline.WaitUntilHandledAsync(ct);

            Assert.Equal("fulfillment", envelope.TargetModule);
            Assert.Equal("fulfillment.order.reserve.v1", envelope.MessageTypeName);
            await WaitForQueueToDrainAsync(connection, queueName, ct);
        }
        finally
        {
            await receiveWorker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_WhenDispatchFails_LeavesDeadLetterHandoffToRabbitMq()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        CancellationToken ct = timeout.Token;
        string queueName = $"bondstone.tests.{Guid.NewGuid():N}";
        string deadLetterExchangeName = $"{queueName}.dlx";
        string deadLetterQueueName = $"{queueName}.dead";
        RabbitMqTransportMessage message = CreateMessage();
        await using IConnection connection = await CreateConnectionAsync(ct);
        await DeclareQueueWithDeadLetterAsync(
            connection,
            queueName,
            deadLetterExchangeName,
            deadLetterQueueName,
            ct);
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            connection,
            new SignalingCommandReceivePipeline(),
            new RecordingEventReceivePipeline(),
            rabbitMq =>
            {
                rabbitMq.ReceiveQueue(queueName);
                rabbitMq.UseReceiveWorker(options => options.RequeueOnFailure = false);
            });
        IHostedService receiveWorker = serviceProvider
            .GetServices<IHostedService>()
            .Single(service => service.GetType().Name == "RabbitMqReceiveWorker");

        await receiveWorker.StartAsync(ct);
        try
        {
            await PublishCommandAsync(connection, queueName, message, ct);

            RabbitMqTransportMessage deadLettered =
                await WaitForMessageAsync(connection, deadLetterQueueName, ct);

            Assert.Equal(message.MessageId, deadLettered.MessageId);
            Assert.Equal(message.MessageTypeName, deadLettered.MessageTypeName);
            RecordingLogSink logs = serviceProvider.GetRequiredService<RecordingLogSink>();
            Assert.Contains(
                logs.Entries,
                entry => entry.Level == LogLevel.Error
                    && entry.EventId.Id == 3001
                    && entry.EventId.Name == "DeliveryHandlingFailed"
                    && entry.Message.Contains(queueName, StringComparison.Ordinal)
                    && entry.Message.Contains("delivery 1", StringComparison.Ordinal)
                    && entry.Message.Contains(message.MessageId, StringComparison.Ordinal)
                    && entry.Message.Contains(message.MessageTypeName, StringComparison.Ordinal)
                    && entry.Message.Contains("Exchange:", StringComparison.Ordinal)
                    && entry.Message.Contains($"RoutingKey: {queueName}", StringComparison.Ordinal)
                    && entry.Message.Contains("Redelivered: False", StringComparison.Ordinal)
                    && entry.Message.Contains("requeue False", StringComparison.Ordinal)
                    && entry.Message.Contains("RabbitMQ retry and dead-letter policy remain broker-owned", StringComparison.Ordinal));
        }
        finally
        {
            await receiveWorker.StopAsync(CancellationToken.None);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ReceiveWorker_WhenEventHasSubscribers_DispatchesEachSubscriberAndAcknowledgesBrokerMessage()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        CancellationToken ct = timeout.Token;
        string queueName = $"bondstone.tests.{Guid.NewGuid():N}";
        var eventPipeline = new SignalingEventReceivePipeline(expectedDeliveryCount: 2);
        await using IConnection connection = await CreateConnectionAsync(ct);
        await DeclareQueueAsync(connection, queueName, ct);
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            connection,
            new SignalingCommandReceivePipeline(),
            eventPipeline,
            rabbitMq =>
            {
                rabbitMq.ReceiveQueue(queueName)
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "fulfillment",
                        "fulfillment.sales-order-projection.v1")
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "billing",
                        "billing.sales-order-projection.v1");
                rabbitMq.UseReceiveWorker();
            });
        IHostedService receiveWorker = serviceProvider
            .GetServices<IHostedService>()
            .Single(service => service.GetType().Name == "RabbitMqReceiveWorker");

        await receiveWorker.StartAsync(ct);
        try
        {
            await PublishCommandAsync(connection, queueName, CreateEventMessage(), ct);

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
            Assert.All(
                deliveries,
                delivery => Assert.Equal(
                    "sales.order.submitted.v1",
                    delivery.Envelope.MessageTypeName));
            await WaitForQueueToDrainAsync(connection, queueName, ct);
        }
        finally
        {
            await receiveWorker.StopAsync(CancellationToken.None);
        }
    }

    private async Task<IConnection> CreateConnectionAsync(
        CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(fixture.ConnectionString),
        };

        return await factory.CreateConnectionAsync(ct);
    }

    private static async Task DeclareQueueAsync(
        IConnection connection,
        string queueName,
        CancellationToken ct)
    {
        await using IChannel channel = await connection.CreateChannelAsync(
            cancellationToken: ct);
        await channel.QueueDeclareAsync(
            queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: ct);
    }

    private static async Task DeclareQueueWithDeadLetterAsync(
        IConnection connection,
        string queueName,
        string deadLetterExchangeName,
        string deadLetterQueueName,
        CancellationToken ct)
    {
        await using IChannel channel = await connection.CreateChannelAsync(
            cancellationToken: ct);
        await channel.ExchangeDeclareAsync(
            deadLetterExchangeName,
            ExchangeType.Direct,
            durable: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: ct);
        await channel.QueueDeclareAsync(
            deadLetterQueueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: ct);
        await channel.QueueBindAsync(
            deadLetterQueueName,
            deadLetterExchangeName,
            deadLetterQueueName,
            arguments: null,
            cancellationToken: ct);
        await channel.QueueDeclareAsync(
            queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["x-dead-letter-exchange"] = deadLetterExchangeName,
                ["x-dead-letter-routing-key"] = deadLetterQueueName,
            },
            cancellationToken: ct);
    }

    private static async Task PublishCommandAsync(
        IConnection connection,
        string queueName,
        RabbitMqTransportMessage message,
        CancellationToken ct)
    {
        await using IChannel channel = await connection.CreateChannelAsync(
            cancellationToken: ct);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            MessageId = message.MessageId,
            CorrelationId = message.CorrelationId,
            Type = message.MessageTypeName,
            Persistent = false,
            Headers = message.Headers.ToDictionary(
                static entry => entry.Key,
                static entry => (object?)entry.Value,
                StringComparer.Ordinal),
        };

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: Encoding.UTF8.GetBytes(message.Body),
            cancellationToken: ct);
    }

    private static async Task WaitForQueueToDrainAsync(
        IConnection connection,
        string queueName,
        CancellationToken ct)
    {
        await using IChannel channel = await connection.CreateChannelAsync(
            cancellationToken: ct);

        while (await channel.MessageCountAsync(queueName, ct) != 0)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
        }
    }

    private static async Task<RabbitMqTransportMessage> WaitForMessageAsync(
        IConnection connection,
        string queueName,
        CancellationToken ct)
    {
        await using IChannel channel = await connection.CreateChannelAsync(
            cancellationToken: ct);

        while (true)
        {
            BasicGetResult? result = await channel.BasicGetAsync(
                queueName,
                autoAck: true,
                cancellationToken: ct);
            if (result is not null)
            {
                return RabbitMqReceivedMessageMapper.FromBodyAndProperties(
                    result.Body,
                    result.BasicProperties);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), ct);
        }
    }

    private static ServiceProvider CreateServiceProvider(
        IConnection connection,
        IModuleCommandReceivePipeline commandPipeline,
        string queueName)
    {
        return CreateServiceProvider(
            connection,
            commandPipeline,
            new RecordingEventReceivePipeline(),
            rabbitMq =>
            {
                rabbitMq.ReceiveQueue(queueName)
                    .AcceptModule("fulfillment");
                rabbitMq.UseReceiveWorker();
            });
    }

    private static ServiceProvider CreateServiceProvider(
        IConnection connection,
        IModuleCommandReceivePipeline commandPipeline,
        IModuleEventReceivePipeline eventPipeline,
        Action<BondstoneRabbitMqTransportBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<RecordingLogSink>();
        services.AddSingleton(typeof(ILogger<>), typeof(RecordingLogger<>));
        services.AddSingleton<IModuleCommandReceivePipeline>(commandPipeline);
        services.AddSingleton(eventPipeline);
        services.AddBondstoneRabbitMqConnection(connection);
        services.AddBondstone(bondstone => bondstone.Outbox.UseRabbitMqTransport(configure));

        return services.BuildServiceProvider();
    }

    private static RabbitMqTransportMessage CreateMessage()
    {
        var envelope = new RabbitMqDurableMessageEnvelope(
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

        return new RabbitMqTransportMessage(
            JsonSerializer.Serialize(envelope),
            messageId,
            envelope.MessageTypeName,
            envelope.DurableOperationId!.Value.ToString("D"),
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [BondstoneRabbitMqHeaders.MessageId] = messageId,
                [BondstoneRabbitMqHeaders.MessageKind] = MessageKind.Command.ToString(),
                [BondstoneRabbitMqHeaders.MessageTypeName] = envelope.MessageTypeName,
            });
    }

    private static RabbitMqTransportMessage CreateEventMessage()
    {
        var envelope = new RabbitMqDurableMessageEnvelope(
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

        return new RabbitMqTransportMessage(
            JsonSerializer.Serialize(envelope),
            messageId,
            envelope.MessageTypeName,
            envelope.DurableOperationId!.Value.ToString("D"),
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [BondstoneRabbitMqHeaders.MessageId] = messageId,
                [BondstoneRabbitMqHeaders.MessageKind] = MessageKind.Event.ToString(),
                [BondstoneRabbitMqHeaders.MessageTypeName] = envelope.MessageTypeName,
            });
    }

    private static DurableInboxHandleResult CreateHandledResult(
        DurableMessageEnvelope envelope)
    {
        return CreateHandledResult(
            envelope,
            envelope.TargetModule!,
            envelope.MessageTypeName);
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

            return ValueTask.FromResult(CreateHandledResult(envelope));
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
            throw new InvalidOperationException("The RabbitMQ integration test does not dispatch events.");
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

    private sealed class RecordingLogSink
    {
        private readonly List<RecordingLogEntry> _entries = [];
        private readonly Lock _lock = new();

        public IReadOnlyCollection<RecordingLogEntry> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries.ToArray();
                }
            }
        }

        public void Add(
            RecordingLogEntry entry)
        {
            lock (_lock)
            {
                _entries.Add(entry);
            }
        }
    }

    private sealed class RecordingLogger<T>(
        RecordingLogSink sink)
        : ILogger<T>
    {
        public IDisposable BeginScope<TState>(
            TState state)
            where TState : notnull
        {
            return NoopDisposable.Instance;
        }

        public bool IsEnabled(
            LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Add(new RecordingLogEntry(
                logLevel,
                eventId,
                formatter(state, exception),
                exception));
        }
    }

    private sealed record RecordingLogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception);

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
