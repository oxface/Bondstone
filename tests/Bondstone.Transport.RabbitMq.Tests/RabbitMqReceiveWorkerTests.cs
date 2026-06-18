using System.Reflection;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.RabbitMq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit;

namespace Bondstone.Transport.RabbitMq.Tests;

public sealed class RabbitMqReceiveWorkerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenReceiverFails_LogsAndNacksUsingRequeueOption()
    {
        IChannel channel = RecordingChannelProxy.Create(out RecordingChannelProxy recorder);
        var receiver = new ConfigurableEnvelopeReceiver
        {
            Exception = new InvalidOperationException("receive failed"),
        };
        await using ServiceProvider provider = BuildProvider(receiver);
        var logger = new RecordingLogger<RabbitMqReceiveWorker>();
        var worker = new RabbitMqReceiveWorker(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            logger);
        var registration = new RabbitMqReceiveWorkerRegistration(
            "fulfillment.commands",
            Binding: null,
            RequeueOnFailure: true,
            ConsumerTag: null,
            RabbitMqReceiveWorkerMode.DirectReceive,
            "rabbitmq:fulfillment.commands");

        await InvokeReceiveAsync(
            worker,
            CreateDelivery(deliveryTag: 42),
            registration);

        Settlement settlement = Assert.Single(recorder.Settlements);
        Assert.Equal("nack", settlement.Kind);
        Assert.Equal((ulong)42, settlement.DeliveryTag);
        Assert.True(settlement.Requeue);
        LogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(2001, entry.EventId.Id);
        Assert.Equal("ReceiveFailed", entry.EventId.Name);
        Assert.IsType<InvalidOperationException>(entry.Exception);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenReceiverSucceeds_AcksOnlyAfterReceiveCompletes()
    {
        IChannel channel = RecordingChannelProxy.Create(out RecordingChannelProxy recorder);
        var receiver = new ConfigurableEnvelopeReceiver();
        await using ServiceProvider provider = BuildProvider(receiver);
        var worker = new RabbitMqReceiveWorker(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<RabbitMqReceiveWorker>());
        var registration = new RabbitMqReceiveWorkerRegistration(
            "fulfillment.commands",
            Binding: null,
            RequeueOnFailure: false,
            ConsumerTag: null,
            RabbitMqReceiveWorkerMode.DirectReceive,
            "rabbitmq:fulfillment.commands");

        Task receiveTask = InvokeReceiveAsync(
            worker,
            CreateDelivery(deliveryTag: 99),
            registration);

        await receiver.ReceiveStarted.Task;
        Assert.Empty(recorder.Settlements);

        receiver.CompleteReceive();
        await receiveTask;

        Settlement settlement = Assert.Single(recorder.Settlements);
        Assert.Equal("ack", settlement.Kind);
        Assert.Equal((ulong)99, settlement.DeliveryTag);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenDurableIncomingInboxIngestionSucceeds_CommitsBeforeAck()
    {
        var order = new List<string>();
        IChannel channel = RecordingChannelProxy.Create(out RecordingChannelProxy recorder, order);
        var store = new RecordingIncomingInboxIngestionStore(order);
        var persistenceScope = new RecordingIncomingInboxIngestionScope(order);
        await using ServiceProvider provider = BuildIngestionProvider(
            store,
            persistenceScope,
            bondstone => bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
            }));
        var worker = new RabbitMqReceiveWorker(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<RabbitMqReceiveWorker>(),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-17T12:00:00+00:00")));
        var registration = new RabbitMqReceiveWorkerRegistration(
            "fulfillment.commands",
            Binding: null,
            RequeueOnFailure: false,
            ConsumerTag: null,
            RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion,
            "rabbitmq:fulfillment.commands");

        Task receiveTask = InvokeReceiveAsync(
            worker,
            CreateDelivery(
                deliveryTag: 100,
                CreateEnvelopeBody(CreateCommandEnvelope())),
            registration);

        await persistenceScope.SaveStarted.Task;
        Assert.Empty(recorder.Settlements);

        persistenceScope.CompleteSave();
        await receiveTask;

        Assert.Equal(["ingest", "save", "ack"], order);
        DurableIncomingInboxRecord record = Assert.Single(store.Records);
        Assert.Equal("fulfillment", record.ReceiverModule);
        Assert.Equal("rabbitmq:fulfillment.commands", record.SourceTransportName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenDurableIncomingInboxAlreadyIngested_AcksWithoutHandlerExecution()
    {
        IChannel channel = RecordingChannelProxy.Create(out RecordingChannelProxy recorder);
        var store = new RecordingIncomingInboxIngestionStore
        {
            Status = DurableIncomingInboxIngestionStatus.AlreadyIngested,
        };
        await using ServiceProvider provider = BuildIngestionProvider(
            store,
            new RecordingIncomingInboxIngestionScope(),
            bondstone => bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
            }));
        var worker = new RabbitMqReceiveWorker(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<RabbitMqReceiveWorker>());
        var registration = new RabbitMqReceiveWorkerRegistration(
            "fulfillment.commands",
            Binding: null,
            RequeueOnFailure: false,
            ConsumerTag: null,
            RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion,
            "rabbitmq:fulfillment.commands");

        await InvokeReceiveAsync(
            worker,
            CreateDelivery(
                deliveryTag: 101,
                CreateEnvelopeBody(CreateCommandEnvelope())),
            registration);

        Settlement settlement = Assert.Single(recorder.Settlements);
        Assert.Equal("ack", settlement.Kind);
        Assert.Empty(provider.GetRequiredService<HandlerCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenDurableIncomingInboxIngestionFails_NacksUsingExistingFailureBehavior()
    {
        IChannel channel = RecordingChannelProxy.Create(out RecordingChannelProxy recorder);
        var store = new RecordingIncomingInboxIngestionStore
        {
            Exception = new InvalidOperationException("ingest failed"),
        };
        await using ServiceProvider provider = BuildIngestionProvider(
            store,
            new RecordingIncomingInboxIngestionScope(),
            bondstone => bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
            }));
        var logger = new RecordingLogger<RabbitMqReceiveWorker>();
        var worker = new RabbitMqReceiveWorker(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            logger);
        var registration = new RabbitMqReceiveWorkerRegistration(
            "fulfillment.commands",
            Binding: null,
            RequeueOnFailure: true,
            ConsumerTag: null,
            RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion,
            "rabbitmq:fulfillment.commands");

        await InvokeReceiveAsync(
            worker,
            CreateDelivery(
                deliveryTag: 102,
                CreateEnvelopeBody(CreateCommandEnvelope())),
            registration);

        Settlement settlement = Assert.Single(recorder.Settlements);
        Assert.Equal("nack", settlement.Kind);
        Assert.True(settlement.Requeue);
        Assert.Empty(provider.GetRequiredService<HandlerCallLog>().Calls);
        Assert.IsType<InvalidOperationException>(Assert.Single(logger.Entries).Exception);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenDurableIncomingInboxCommandIngestion_UsesTargetModuleAndHandlerIdentity()
    {
        IChannel channel = RecordingChannelProxy.Create(out _);
        var store = new RecordingIncomingInboxIngestionStore();
        await using ServiceProvider provider = BuildIngestionProvider(
            store,
            new RecordingIncomingInboxIngestionScope(),
            bondstone => bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>(
                    "fulfillment.reserve-inventory-handler.v2");
            }));
        var worker = new RabbitMqReceiveWorker(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<RabbitMqReceiveWorker>());
        var registration = new RabbitMqReceiveWorkerRegistration(
            "fulfillment.commands",
            Binding: null,
            RequeueOnFailure: false,
            ConsumerTag: null,
            RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion,
            "rabbitmq:fulfillment.commands");

        await InvokeReceiveAsync(
            worker,
            CreateDelivery(
                deliveryTag: 103,
                CreateEnvelopeBody(CreateCommandEnvelope())),
            registration);

        DurableIncomingInboxRecord record = Assert.Single(store.Records);
        Assert.Equal(CreateCommandEnvelope().MessageId, record.Key.MessageId);
        Assert.Equal("fulfillment", record.Key.ReceiverModule);
        Assert.Equal("fulfillment.reserve-inventory-handler.v2", record.Key.HandlerIdentity);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenDurableIncomingInboxEventIngestion_UsesSubscriberBinding()
    {
        IChannel channel = RecordingChannelProxy.Create(out _);
        var store = new RecordingIncomingInboxIngestionStore();
        await using ServiceProvider provider = BuildIngestionProvider(
            store,
            new RecordingIncomingInboxIngestionScope(),
            bondstone => bondstone.Module("billing", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Events.RegisterSubscriber<OrderPlacedEvent, OrderPlacedHandler>(
                    "billing.order-placed-projection.v1");
            }));
        var worker = new RabbitMqReceiveWorker(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<RabbitMqReceiveWorker>());
        var registration = new RabbitMqReceiveWorkerRegistration(
            "billing.order-placed",
            new DurableEnvelopeReceiveBinding(
                "billing",
                "billing.order-placed-projection.v1"),
            RequeueOnFailure: false,
            ConsumerTag: null,
            RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion,
            "rabbitmq:billing.order-placed");

        await InvokeReceiveAsync(
            worker,
            CreateDelivery(
                deliveryTag: 104,
                CreateEnvelopeBody(CreateEventEnvelope())),
            registration);

        DurableIncomingInboxRecord record = Assert.Single(store.Records);
        Assert.Equal(CreateEventEnvelope().MessageId, record.Key.MessageId);
        Assert.Equal("billing", record.Key.ReceiverModule);
        Assert.Equal("billing.order-placed-projection.v1", record.Key.HandlerIdentity);
        Assert.Empty(provider.GetRequiredService<HandlerCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenDurableIncomingInboxIngestion_PreservesEnvelopeFields()
    {
        IChannel channel = RecordingChannelProxy.Create(out _);
        var store = new RecordingIncomingInboxIngestionStore();
        await using ServiceProvider provider = BuildIngestionProvider(
            store,
            new RecordingIncomingInboxIngestionScope(),
            bondstone => bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
            }));
        var worker = new RabbitMqReceiveWorker(
            channel,
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<RabbitMqReceiveWorker>(),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-17T12:03:00+00:00")));
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "fulfillment.reserve-inventory.v1",
            "ordering",
            "fulfillment",
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-17T11:59:00+00:00"),
            Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"),
            new MessageTraceContext(
                "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
                "state=ok",
                "tenant=alpha"),
            Guid.Parse("cccccccc-3333-3333-3333-333333333333"),
            "partition-A",
            """{"priority":"high"}""");
        var registration = new RabbitMqReceiveWorkerRegistration(
            "fulfillment.commands",
            Binding: null,
            RequeueOnFailure: false,
            ConsumerTag: null,
            RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion,
            "rabbitmq:fulfillment.commands");

        await InvokeReceiveAsync(
            worker,
            CreateDelivery(
                deliveryTag: 105,
                CreateEnvelopeBody(envelope)),
            registration);

        DurableIncomingInboxRecord record = Assert.Single(store.Records);
        Assert.Equal(DateTimeOffset.Parse("2026-06-17T12:03:00+00:00"), record.IngestedAtUtc);
        Assert.Equal(envelope.MessageId, record.Envelope.MessageId);
        Assert.Equal(envelope.MessageKind, record.Envelope.MessageKind);
        Assert.Equal(envelope.MessageTypeName, record.Envelope.MessageTypeName);
        Assert.Equal(envelope.SourceModule, record.Envelope.SourceModule);
        Assert.Equal(envelope.TargetModule, record.Envelope.TargetModule);
        Assert.Equal(envelope.DurableOperationId, record.Envelope.DurableOperationId);
        Assert.Equal(envelope.TraceContext, record.Envelope.TraceContext);
        Assert.Equal(envelope.CausationId, record.Envelope.CausationId);
        Assert.Equal(envelope.PartitionKey, record.Envelope.PartitionKey);
        Assert.Equal(envelope.Payload, record.Envelope.Payload);
        Assert.Equal(envelope.Metadata, record.Envelope.Metadata);
        Assert.Equal(envelope.CreatedAtUtc, record.Envelope.CreatedAtUtc);
    }

    private static ServiceProvider BuildProvider(
        IDurableEnvelopeReceiver receiver)
    {
        var services = new ServiceCollection();
        services.AddSingleton(receiver);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static ServiceProvider BuildIngestionProvider(
        IDurableIncomingInboxIngestionStore store,
        IDurableIncomingInboxIngestionPersistenceScope persistenceScope,
        Action<Bondstone.Configuration.BondstoneBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(persistenceScope);
        services.AddSingleton<HandlerCallLog>();
        services.AddBondstone(configure);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static BasicDeliverEventArgs CreateDelivery(
        ulong deliveryTag,
        ReadOnlyMemory<byte>? body = null)
    {
        return new BasicDeliverEventArgs(
            consumerTag: "consumer",
            deliveryTag,
            redelivered: false,
            exchange: string.Empty,
            routingKey: "fulfillment.commands",
            properties: new BasicProperties(),
            body: body ?? new byte[] { 1, 2, 3 },
            cancellationToken: CancellationToken.None);
    }

    private static ReadOnlyMemory<byte> CreateEnvelopeBody(
        DurableMessageEnvelope envelope)
    {
        return new SystemTextJsonDurableMessageEnvelopeSerializer()
            .SerializeToUtf8Bytes(envelope);
    }

    private static Task InvokeReceiveAsync(
        RabbitMqReceiveWorker worker,
        BasicDeliverEventArgs args,
        RabbitMqReceiveWorkerRegistration registration)
    {
        MethodInfo method = typeof(RabbitMqReceiveWorker).GetMethod(
            "ReceiveAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)method.Invoke(
            worker,
            [args, registration, CancellationToken.None])!;
    }

    private sealed class ConfigurableEnvelopeReceiver : IDurableEnvelopeReceiver
    {
        private readonly TaskCompletionSource _completeReceive =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReceiveStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Exception? Exception { get; init; }

        public void CompleteReceive()
        {
            _completeReceive.TrySetResult();
        }

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            DurableMessageEnvelope envelope,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            return CompleteAsync();
        }

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            ReadOnlyMemory<byte> utf8Json,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            return CompleteAsync();
        }

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            string json,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            return CompleteAsync();
        }

        public ValueTask<DurableInboxHandleResult> ReceiveCommandAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            return CompleteAsync();
        }

        public ValueTask<DurableInboxHandleResult> ReceiveEventAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            return CompleteAsync();
        }

        private async ValueTask<DurableInboxHandleResult> CompleteAsync()
        {
            ReceiveStarted.TrySetResult();
            if (Exception is not null)
            {
                throw Exception;
            }

            await _completeReceive.Task;
            return CreateHandledResult();
        }
    }

    private class RecordingChannelProxy : DispatchProxy
    {
        private List<string>? _order;

        public List<Settlement> Settlements { get; } = [];

        public static IChannel Create(
            out RecordingChannelProxy recorder,
            List<string>? order = null)
        {
            IChannel channel = DispatchProxy.Create<IChannel, RecordingChannelProxy>();
            recorder = (RecordingChannelProxy)(object)channel;
            recorder._order = order;
            return channel;
        }

        protected override object? Invoke(
            MethodInfo? targetMethod,
            object?[]? args)
        {
            if (targetMethod?.Name == nameof(IChannel.BasicAckAsync))
            {
                _order?.Add("ack");
                Settlements.Add(new Settlement(
                    "ack",
                    (ulong)args![0]!,
                    Requeue: null));
            }

            if (targetMethod?.Name == nameof(IChannel.BasicNackAsync))
            {
                _order?.Add("nack");
                Settlements.Add(new Settlement(
                    "nack",
                    (ulong)args![0]!,
                    (bool)args[2]!));
            }

            return CreateDefaultReturn(targetMethod?.ReturnType);
        }

        private static object? CreateDefaultReturn(
            Type? returnType)
        {
            if (returnType is null || returnType == typeof(void))
            {
                return null;
            }

            if (returnType == typeof(ValueTask))
            {
                return ValueTask.CompletedTask;
            }

            if (returnType == typeof(Task))
            {
                return Task.CompletedTask;
            }

            if (returnType.IsGenericType
                && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                return Activator.CreateInstance(returnType);
            }

            if (returnType.IsValueType)
            {
                return Activator.CreateInstance(returnType);
            }

            return null;
        }
    }

    private sealed class RecordingIncomingInboxIngestionStore(
        List<string>? order = null)
        : IDurableIncomingInboxIngestionStore
    {
        private readonly List<string>? _order = order;

        public List<DurableIncomingInboxRecord> Records { get; } = [];

        public DurableIncomingInboxIngestionStatus Status { get; init; } =
            DurableIncomingInboxIngestionStatus.Ingested;

        public Exception? Exception { get; init; }

        public ValueTask<DurableIncomingInboxIngestionResult> IngestAsync(
            DurableIncomingInboxRecord record,
            CancellationToken ct = default)
        {
            _order?.Add("ingest");
            if (Exception is not null)
            {
                throw Exception;
            }

            Records.Add(record);
            return ValueTask.FromResult(
                new DurableIncomingInboxIngestionResult(
                    Status,
                    record));
        }
    }

    private sealed class RecordingIncomingInboxIngestionScope(
        List<string>? order = null)
        : IDurableIncomingInboxIngestionPersistenceScope
    {
        private readonly List<string>? _order = order;
        private readonly TaskCompletionSource _completeSave =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource SaveStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ValueTask<TResult> ExecuteAsync<TResult>(
            Func<IDurableIncomingInboxIngestionPersistenceScope, CancellationToken, ValueTask<TResult>> operation,
            CancellationToken ct = default)
        {
            return operation(this, ct);
        }

        public async ValueTask SaveChangesAsync(
            CancellationToken ct = default)
        {
            _order?.Add("save");
            SaveStarted.TrySetResult();
            if (_order is not null)
            {
                await _completeSave.Task.WaitAsync(ct);
            }
        }

        public void CompleteSave()
        {
            _completeSave.TrySetResult();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }

    private sealed class HandlerCallLog
    {
        public List<string> Calls { get; } = [];
    }

    [DurableCommandIdentity("fulfillment.reserve-inventory.v1")]
    private sealed record ReserveInventoryCommand(string OrderId) : IDurableCommand;

    private sealed class ReserveInventoryHandler(HandlerCallLog log)
        : ICommandHandler<ReserveInventoryCommand>
    {
        public ValueTask HandleAsync(
            ReserveInventoryCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add(command.OrderId);
            return ValueTask.CompletedTask;
        }
    }

    [IntegrationEventIdentity("ordering.order-placed.v1")]
    private sealed record OrderPlacedEvent(string OrderId) : IIntegrationEvent;

    private sealed class OrderPlacedHandler(HandlerCallLog log)
        : IIntegrationEventHandler<OrderPlacedEvent>
    {
        public ValueTask HandleAsync(
            OrderPlacedEvent integrationEvent,
            CancellationToken ct = default)
        {
            log.Calls.Add(integrationEvent.OrderId);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(
            TState state)
            where TState : notnull
        {
            return NullScope.Instance;
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
            Entries.Add(new LogEntry(eventId, exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }

    private static DurableInboxHandleResult CreateHandledResult()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var record = new DurableInboxRecord(
            DurableInboxMessageKey.ForCommandHandler(
                Guid.NewGuid(),
                "fulfillment",
                "fulfillment.reserve-inventory.v1"),
            now,
            now);
        return new DurableInboxHandleResult(
            DurableInboxHandleStatus.Handled,
            record);
    }

    private static DurableMessageEnvelope CreateCommandEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "fulfillment.reserve-inventory.v1",
            "ordering",
            "fulfillment",
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-17T11:58:00+00:00"));
    }

    private static DurableMessageEnvelope CreateEventEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            MessageKind.Event,
            "ordering.order-placed.v1",
            "ordering",
            targetModule: null,
            payload: """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-17T11:58:00+00:00"));
    }

    private sealed record Settlement(
        string Kind,
        ulong DeliveryTag,
        bool? Requeue);

    private sealed record LogEntry(
        EventId EventId,
        Exception? Exception);
}
