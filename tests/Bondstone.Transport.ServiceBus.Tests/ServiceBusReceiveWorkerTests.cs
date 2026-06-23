using System.Reflection;
using Azure.Messaging.ServiceBus;
using Bondstone.Configuration;
using Bondstone.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Bondstone.Transport.ServiceBus.Tests;

public sealed class ServiceBusReceiveWorkerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessErrorAsync_LogsReceiveFailureEvent()
    {
        await using ServiceProvider provider =
            BuildProvider(new ConfigurableEnvelopeReceiver());
        var logger = new RecordingLogger<ServiceBusReceiveWorker>();
        var worker = new ServiceBusReceiveWorker(
            CreateClient(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            logger);
        var exception = new InvalidOperationException("processor failed");

        await InvokeProcessErrorAsync(
            worker,
            new ProcessErrorEventArgs(
                exception,
                ServiceBusErrorSource.Receive,
                "fully.qualified",
                "fulfillment.commands",
                "processor",
                CancellationToken.None));

        LogEntry entry = Assert.Single(logger.Entries);
        Assert.Equal(3001, entry.EventId.Id);
        Assert.Equal("ReceiveFailed", entry.EventId.Name);
        Assert.Same(exception, entry.Exception);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessMessageAsync_WhenDurableIncomingInboxIngestionSucceeds_CompletesOnlyAfterSave()
    {
        var order = new List<string>();
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
        var worker = new ServiceBusReceiveWorker(
            CreateClient(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<ServiceBusReceiveWorker>(),
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-19T12:00:00+00:00")));
        var args = new RecordingProcessMessageEventArgs(
            CreateReceivedMessage(CreateEnvelopeBody(CreateCommandEnvelope())))
        {
            Order = order,
        };
        var registration = new ServiceBusReceiveWorkerRegistration(
            QueueName: "fulfillment.commands",
            TopicName: null,
            SubscriptionName: null,
            Binding: null,
            new ServiceBusProcessorOptions(),
            "servicebus:fulfillment.commands");

        Task processTask = InvokeProcessMessageAsync(
            worker,
            args,
            registration);

        Task startedTask = await Task.WhenAny(
            persistenceScope.SaveStarted.Task,
            processTask);
        if (startedTask == processTask)
        {
            await processTask;
        }

        Assert.Equal(0, args.CompleteCount);

        persistenceScope.CompleteSave();
        await processTask;

        Assert.Equal(1, args.CompleteCount);
        Assert.Equal(["ingest", "save", "complete"], order);
        DurableIncomingInboxRecord record = Assert.Single(store.Records);
        Assert.Equal("fulfillment", record.ReceiverModule);
        Assert.Equal("servicebus:fulfillment.commands", record.SourceTransportName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessMessageAsync_WhenDurableIncomingInboxIngestionFails_DoesNotCompleteMessage()
    {
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
        var worker = new ServiceBusReceiveWorker(
            CreateClient(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<ServiceBusReceiveWorker>());
        var args = new RecordingProcessMessageEventArgs(
            CreateReceivedMessage(CreateEnvelopeBody(CreateCommandEnvelope())));
        var registration = new ServiceBusReceiveWorkerRegistration(
            QueueName: "fulfillment.commands",
            TopicName: null,
            SubscriptionName: null,
            Binding: null,
            new ServiceBusProcessorOptions(),
            "servicebus:fulfillment.commands");

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await InvokeProcessMessageAsync(
                worker,
                args,
                registration));

        Assert.Equal(0, args.CompleteCount);
        Assert.Empty(provider.GetRequiredService<HandlerCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessMessageAsync_WhenDurableIncomingInboxAlreadyIngested_CompletesWithoutHandlerExecution()
    {
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
            }),
            services => services.AddSingleton<IDurableEnvelopeReceiver>(
                new ThrowingEnvelopeReceiver()));
        var worker = new ServiceBusReceiveWorker(
            CreateClient(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<ServiceBusReceiveWorker>());
        var args = new RecordingProcessMessageEventArgs(
            CreateReceivedMessage(CreateEnvelopeBody(CreateCommandEnvelope())));
        var registration = new ServiceBusReceiveWorkerRegistration(
            QueueName: "fulfillment.commands",
            TopicName: null,
            SubscriptionName: null,
            Binding: null,
            new ServiceBusProcessorOptions(),
            "servicebus:fulfillment.commands");

        await InvokeProcessMessageAsync(
            worker,
            args,
            registration);

        Assert.Equal(1, args.CompleteCount);
        Assert.Empty(provider.GetRequiredService<HandlerCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessMessageAsync_WhenDurableIncomingInboxEventIngestion_UsesSubscriberBinding()
    {
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
        var worker = new ServiceBusReceiveWorker(
            CreateClient(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<ServiceBusReceiveWorker>());
        var args = new RecordingProcessMessageEventArgs(
            CreateReceivedMessage(CreateEnvelopeBody(CreateEventEnvelope())));
        var registration = new ServiceBusReceiveWorkerRegistration(
            QueueName: null,
            TopicName: "orders",
            SubscriptionName: "billing",
            new DurableEnvelopeReceiveBinding(
                "billing",
                "billing.order-placed-projection.v1"),
            new ServiceBusProcessorOptions(),
            "servicebus:orders/billing");

        await InvokeProcessMessageAsync(
            worker,
            args,
            registration);

        DurableIncomingInboxRecord record = Assert.Single(store.Records);
        Assert.Equal(CreateEventEnvelope().MessageId, record.Key.MessageId);
        Assert.Equal("billing", record.Key.ReceiverModule);
        Assert.Equal("billing.order-placed-projection.v1", record.Key.HandlerIdentity);
        Assert.Equal(1, args.CompleteCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessMessageAsync_WhenDurableIncomingInboxEventIngestionHasNoBinding_ThrowsSetupCode()
    {
        var store = new RecordingIncomingInboxIngestionStore();
        await using ServiceProvider provider = BuildIngestionProvider(
            store,
            new RecordingIncomingInboxIngestionScope(),
            bondstone => bondstone.Module("billing", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
            }));
        var worker = new ServiceBusReceiveWorker(
            CreateClient(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<ServiceBusReceiveWorker>());
        var args = new RecordingProcessMessageEventArgs(
            CreateReceivedMessage(CreateEnvelopeBody(CreateEventEnvelope())));
        var registration = new ServiceBusReceiveWorkerRegistration(
            QueueName: null,
            TopicName: "orders",
            SubscriptionName: "billing",
            Binding: null,
            new ServiceBusProcessorOptions(),
            "servicebus:orders/billing");

        ArgumentException exception = await Assert.ThrowsAnyAsync<ArgumentException>(
            async () => await InvokeProcessMessageAsync(
                worker,
                args,
                registration));

        Assert.Equal(
            BondstoneSetupCodes.MissingReceiveBinding,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
        Assert.Equal(0, args.CompleteCount);
        Assert.Empty(store.Records);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ProcessMessageAsync_WhenDurableIncomingInboxEventIngestionBindingHasNoSubscriber_ThrowsSetupCode()
    {
        var store = new RecordingIncomingInboxIngestionStore();
        await using ServiceProvider provider = BuildIngestionProvider(
            store,
            new RecordingIncomingInboxIngestionScope(),
            bondstone => bondstone.Module("billing", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
            }));
        var worker = new ServiceBusReceiveWorker(
            CreateClient(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<ServiceBusReceiveWorker>());
        var args = new RecordingProcessMessageEventArgs(
            CreateReceivedMessage(CreateEnvelopeBody(CreateEventEnvelope())));
        var registration = new ServiceBusReceiveWorkerRegistration(
            QueueName: null,
            TopicName: "orders",
            SubscriptionName: "billing",
            new DurableEnvelopeReceiveBinding(
                "billing",
                "billing.order-placed-projection.v1"),
            new ServiceBusProcessorOptions(),
            "servicebus:orders/billing");

        InvalidOperationException exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(
            async () => await InvokeProcessMessageAsync(
                worker,
                args,
                registration));

        Assert.Equal(
            BondstoneSetupCodes.MissingReceiveBinding,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
        Assert.Equal(0, args.CompleteCount);
        Assert.Empty(store.Records);
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
        Action<BondstoneBuilder> configure,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton(store);
        services.AddSingleton(persistenceScope);
        services.AddSingleton<HandlerCallLog>();
        configureServices?.Invoke(services);
        services.AddBondstone(configure);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static ServiceBusClient CreateClient()
    {
        return new ServiceBusClient(
            "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test");
    }

    private static ServiceBusReceivedMessage CreateReceivedMessage(
        ReadOnlyMemory<byte>? body = null)
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromBytes(body?.ToArray() ?? [1, 2, 3]));
    }

    private static ReadOnlyMemory<byte> CreateEnvelopeBody(
        DurableMessageEnvelope envelope)
    {
        return new SystemTextJsonDurableMessageEnvelopeSerializer()
            .SerializeToUtf8Bytes(envelope);
    }

    private static Task InvokeProcessMessageAsync(
        ServiceBusReceiveWorker worker,
        ProcessMessageEventArgs args,
        ServiceBusReceiveWorkerRegistration registration)
    {
        MethodInfo method = typeof(ServiceBusReceiveWorker).GetMethod(
            "ProcessMessageAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)method.Invoke(worker, [args, registration])!;
    }

    private static Task InvokeProcessErrorAsync(
        ServiceBusReceiveWorker worker,
        ProcessErrorEventArgs args)
    {
        MethodInfo method = typeof(ServiceBusReceiveWorker).GetMethod(
            "ProcessErrorAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        return (Task)method.Invoke(worker, [args])!;
    }

    private sealed class ConfigurableEnvelopeReceiver : IDurableEnvelopeReceiver
    {
        private readonly TaskCompletionSource _completeReceive =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReceiveStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

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
            await _completeReceive.Task;
            return CreateHandledResult();
        }
    }

    private sealed class ThrowingEnvelopeReceiver : IDurableEnvelopeReceiver
    {
        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            DurableMessageEnvelope envelope,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException("Direct receive should not run during ingestion.");
        }

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            ReadOnlyMemory<byte> utf8Json,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException("Direct receive should not run during ingestion.");
        }

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            string json,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException("Direct receive should not run during ingestion.");
        }

        public ValueTask<DurableInboxHandleResult> ReceiveCommandAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException("Command receive should not run during ingestion.");
        }

        public ValueTask<DurableInboxHandleResult> ReceiveEventAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            throw new InvalidOperationException("Event receive should not run during ingestion.");
        }
    }

    private sealed class RecordingProcessMessageEventArgs(
        ServiceBusReceivedMessage message)
        : ProcessMessageEventArgs(
            message,
            new TestServiceBusReceiver(),
            CancellationToken.None)
    {
        public int CompleteCount { get; private set; }

        public override Task CompleteMessageAsync(
            ServiceBusReceivedMessage message,
            CancellationToken cancellationToken = default)
        {
            CompleteCount++;
            Order?.Add("complete");
            return Task.CompletedTask;
        }

        public List<string>? Order { get; init; }
    }

    private sealed class TestServiceBusReceiver : ServiceBusReceiver
    {
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

    private sealed record LogEntry(
        EventId EventId,
        Exception? Exception);

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

        public int SaveCount { get; private set; }

        public void CompleteSave()
        {
            _completeSave.TrySetResult();
        }

        public async ValueTask<TResult> ExecuteAsync<TResult>(
            Func<IDurableIncomingInboxIngestionPersistenceScope, CancellationToken, ValueTask<TResult>> operation,
            CancellationToken ct = default)
        {
            return await operation(this, ct);
        }

        public async ValueTask SaveChangesAsync(
            CancellationToken ct = default)
        {
            _order?.Add("save");
            SaveCount++;
            SaveStarted.TrySetResult();
            if (_order is not null)
            {
                await _completeSave.Task.WaitAsync(ct);
            }
        }
    }

    private static DurableMessageEnvelope CreateCommandEnvelope()
    {
        var serializer = new SystemTextJsonDurablePayloadSerializer();
        return new DurableMessageEnvelope(
            Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "fulfillment.reserve-inventory.v1",
            "ordering",
            "fulfillment",
            serializer.Serialize(new ReserveInventoryCommand("A-100")),
            DateTimeOffset.Parse("2026-06-19T11:59:00+00:00"));
    }

    private static DurableMessageEnvelope CreateEventEnvelope()
    {
        var serializer = new SystemTextJsonDurablePayloadSerializer();
        return new DurableMessageEnvelope(
            Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222"),
            MessageKind.Event,
            "ordering.order-placed.v1",
            "ordering",
            targetModule: null,
            serializer.Serialize(new OrderPlacedEvent("A-100")),
            DateTimeOffset.Parse("2026-06-19T11:59:00+00:00"));
    }

    [DurableCommandIdentity("fulfillment.reserve-inventory.v1")]
    private sealed record ReserveInventoryCommand(string OrderId) : IDurableCommand;

    private sealed class ReserveInventoryHandler(HandlerCallLog callLog)
        : ICommandHandler<ReserveInventoryCommand>
    {
        public ValueTask HandleAsync(
            ReserveInventoryCommand command,
            CancellationToken ct = default)
        {
            callLog.Calls.Add($"reserve:{command.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    [IntegrationEventIdentity("ordering.order-placed.v1")]
    private sealed record OrderPlacedEvent(string OrderId) : IIntegrationEvent;

    private sealed class OrderPlacedHandler(HandlerCallLog callLog)
        : IIntegrationEventHandler<OrderPlacedEvent>
    {
        public ValueTask HandleAsync(
            OrderPlacedEvent integrationEvent,
            CancellationToken ct = default)
        {
            callLog.Calls.Add($"order:{integrationEvent.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class HandlerCallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
