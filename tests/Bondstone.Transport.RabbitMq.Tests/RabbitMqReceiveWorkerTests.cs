using System.Reflection;
using Bondstone.Messaging;
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
            ConsumerTag: null);

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
            ConsumerTag: null);

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

    private static ServiceProvider BuildProvider(
        IDurableEnvelopeReceiver receiver)
    {
        var services = new ServiceCollection();
        services.AddSingleton(receiver);
        return services.BuildServiceProvider(validateScopes: true);
    }

    private static BasicDeliverEventArgs CreateDelivery(
        ulong deliveryTag)
    {
        return new BasicDeliverEventArgs(
            consumerTag: "consumer",
            deliveryTag,
            redelivered: false,
            exchange: string.Empty,
            routingKey: "fulfillment.commands",
            properties: new BasicProperties(),
            body: new byte[] { 1, 2, 3 },
            cancellationToken: CancellationToken.None);
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
        public List<Settlement> Settlements { get; } = [];

        public static IChannel Create(
            out RecordingChannelProxy recorder)
        {
            IChannel channel = DispatchProxy.Create<IChannel, RecordingChannelProxy>();
            recorder = (RecordingChannelProxy)(object)channel;
            return channel;
        }

        protected override object? Invoke(
            MethodInfo? targetMethod,
            object?[]? args)
        {
            if (targetMethod?.Name == nameof(IChannel.BasicAckAsync))
            {
                Settlements.Add(new Settlement(
                    "ack",
                    (ulong)args![0]!,
                    Requeue: null));
            }

            if (targetMethod?.Name == nameof(IChannel.BasicNackAsync))
            {
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

    private sealed record Settlement(
        string Kind,
        ulong DeliveryTag,
        bool? Requeue);

    private sealed record LogEntry(
        EventId EventId,
        Exception? Exception);
}
