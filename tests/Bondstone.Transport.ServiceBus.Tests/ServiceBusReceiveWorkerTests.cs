using System.Reflection;
using Azure.Messaging.ServiceBus;
using Bondstone.Messaging;
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
    public async Task ProcessMessageAsync_WhenReceiverSucceeds_CompletesOnlyAfterReceiveCompletes()
    {
        var receiver = new ConfigurableEnvelopeReceiver();
        await using ServiceProvider provider = BuildProvider(receiver);
        var worker = new ServiceBusReceiveWorker(
            CreateClient(),
            provider.GetRequiredService<IServiceScopeFactory>(),
            [],
            new RecordingLogger<ServiceBusReceiveWorker>());
        var args = new RecordingProcessMessageEventArgs(
            CreateReceivedMessage());
        var registration = new ServiceBusReceiveWorkerRegistration(
            QueueName: "fulfillment.commands",
            TopicName: null,
            SubscriptionName: null,
            Binding: null,
            new ServiceBusProcessorOptions());

        Task processTask = InvokeProcessMessageAsync(
            worker,
            args,
            registration);

        await receiver.ReceiveStarted.Task;
        Assert.Equal(0, args.CompleteCount);

        receiver.CompleteReceive();
        await processTask;

        Assert.Equal(1, args.CompleteCount);
    }

    private static ServiceProvider BuildProvider(
        IDurableEnvelopeReceiver receiver)
    {
        var services = new ServiceCollection();
        services.AddSingleton(receiver);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static ServiceBusClient CreateClient()
    {
        return new ServiceBusClient(
            "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test");
    }

    private static ServiceBusReceivedMessage CreateReceivedMessage()
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromBytes([1, 2, 3]));
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
            return Task.CompletedTask;
        }
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
}
