using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Serialization.Json;
using Rebus.Transport.InMem;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Inbox;

public sealed class RebusReceiveTransportTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendLocal_WhenTypedCommandIsReceived_HandlesThroughRebusWorker()
    {
        var handled = new TaskCompletionSource<HandledCommandResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var registry = new MessageTypeRegistry();
        registry.Register<ReserveOrderCommand>("fulfillment.order.reserve.v1");
        var inboxExecutor = new CapturingRebusInboxExecutor(DurableInboxHandleStatus.Handled);
        var pipeline = new RebusTypedCommandReceivePipeline(registry, inboxExecutor);

        using var activator = new BuiltinHandlerActivator();
        activator.Handle<RebusDurableMessageEnvelope>(
            async envelope =>
            {
                var commitCalls = 0;
                ReserveOrderCommand? handledCommand = null;

                DurableInboxHandleResult result =
                    await pipeline.HandleOnceAsync<ReserveOrderCommand>(
                        envelope,
                        "transport-receive-handler",
                        (command, _) =>
                        {
                            handledCommand = command;
                            return ValueTask.CompletedTask;
                        },
                        _ =>
                        {
                            commitCalls++;
                            return ValueTask.CompletedTask;
                        });

                handled.SetResult(new HandledCommandResult(
                    handledCommand!,
                    result,
                    commitCalls));
            });

        var network = new InMemNetwork();
        using IBus bus = StartBus(activator, network, "bondstone-receive");

        await bus.SendLocal(CreateEnvelope());

        HandledCommandResult handledResult = await WaitAsync(handled.Task);

        Assert.Equal("A-100", handledResult.Command.OrderId);
        Assert.Equal(DurableInboxHandleStatus.Handled, handledResult.Result?.Status);
        Assert.Equal(1, handledResult.CommitCalls);
        Assert.Equal(CreateEnvelope().MessageId, inboxExecutor.Envelope?.MessageId);
        Assert.Equal("transport-receive-handler", inboxExecutor.HandlerIdentity);
        Assert.Equal(1, inboxExecutor.HandlerCalls);
        Assert.Equal(1, inboxExecutor.CommitCalls);
        Assert.Equal(0, network.Count("bondstone-receive"));
        Assert.Equal(0, network.Count("error"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendLocal_WhenTypedPipelineFails_MovesMessageToErrorQueue()
    {
        var failed = new TaskCompletionSource<Exception>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var registry = new MessageTypeRegistry();
        var inboxExecutor = new CapturingRebusInboxExecutor(DurableInboxHandleStatus.Handled);
        var pipeline = new RebusTypedCommandReceivePipeline(registry, inboxExecutor);

        using var activator = new BuiltinHandlerActivator();
        activator.Handle<RebusDurableMessageEnvelope>(
            async envelope =>
            {
                try
                {
                    await pipeline.HandleOnceAsync<ReserveOrderCommand>(
                        envelope,
                        "transport-receive-handler",
                        (_, _) => ValueTask.CompletedTask,
                        _ => ValueTask.CompletedTask);
                }
                catch (Exception exception)
                {
                    failed.TrySetResult(exception);
                    throw;
                }
            });

        var network = new InMemNetwork();
        using IBus bus = StartBus(activator, network, "bondstone-receive-failure");

        await bus.SendLocal(CreateEnvelope());

        Exception exception = await WaitAsync(failed.Task);
        await WaitUntilAsync(() => network.Count("error") == 1);

        Assert.IsType<KeyNotFoundException>(exception);
        Assert.Null(inboxExecutor.Envelope);
        Assert.Equal(0, network.Count("bondstone-receive-failure"));
    }

    private static IBus StartBus(
        BuiltinHandlerActivator activator,
        InMemNetwork network,
        string inputQueueName)
    {
        return Configure
            .With(activator)
            .Transport(transport => transport.UseInMemoryTransport(network, inputQueueName))
            .Serialization(serializer => serializer.UseSystemTextJson())
            .Options(options => options.RetryStrategy("error", maxDeliveryAttempts: 1))
            .Start();
    }

    private static async Task<T> WaitAsync<T>(Task<T> task)
    {
        Task completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        if (completed != task)
        {
            throw new TimeoutException("Timed out waiting for Rebus receive test signal.");
        }

        return await task;
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        while (!condition())
        {
            if (timeout.IsCancellationRequested)
            {
                throw new TimeoutException("Timed out waiting for Rebus in-memory transport state.");
            }

            await Task.Delay(25);
        }
    }

    private static RebusDurableMessageEnvelope CreateEnvelope()
    {
        return new RebusDurableMessageEnvelope(
            Guid.Parse("3f1a9e26-75d4-4a7d-bb48-ae453f5e5e02"),
            MessageKind.Command.ToString(),
            "fulfillment.order.reserve.v1",
            "sales",
            "fulfillment",
            """{"orderId":"A-100"}""",
            null,
            DateTimeOffset.Parse("2026-06-05T12:00:00+00:00"),
            DurableOperationId: null,
            TraceParent: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
            TraceState: null,
            TraceBaggage: null,
            CausationId: null,
            PartitionKey: "orders/A-100");
    }

    public sealed record ReserveOrderCommand(string OrderId) : IDurableCommand;

    private sealed record HandledCommandResult(
        ReserveOrderCommand Command,
        DurableInboxHandleResult? Result,
        int CommitCalls);

    private sealed class CapturingRebusInboxExecutor(DurableInboxHandleStatus status)
        : IRebusDurableInboxHandlerExecutor
    {
        public RebusDurableMessageEnvelope? Envelope { get; private set; }

        public string? HandlerIdentity { get; private set; }

        public int HandlerCalls { get; private set; }

        public int CommitCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            RebusDurableMessageEnvelope envelope,
            string handlerIdentity,
            Func<DurableMessageEnvelope, CancellationToken, ValueTask> handler,
            Func<CancellationToken, ValueTask> commit,
            CancellationToken ct = default)
        {
            Envelope = envelope;
            HandlerIdentity = handlerIdentity;

            var record = new DurableInboxRecord(
                new DurableInboxMessageKey(
                    envelope.MessageId,
                    envelope.TargetModule!,
                    handlerIdentity),
                DateTimeOffset.Parse("2026-06-05T12:01:00+00:00"));

            if (status == DurableInboxHandleStatus.Handled)
            {
                HandlerCalls++;
                await handler(CreateDurableEnvelope(envelope), ct);
                CommitCalls++;
                await commit(ct);
            }

            return new DurableInboxHandleResult(status, record);
        }

        private static DurableMessageEnvelope CreateDurableEnvelope(
            RebusDurableMessageEnvelope envelope)
        {
            return new DurableMessageEnvelope(
                envelope.MessageId,
                MessageKind.Command,
                envelope.MessageTypeName,
                envelope.SourceModule,
                envelope.TargetModule,
                envelope.Payload,
                envelope.CreatedAtUtc,
                durableOperationId: envelope.DurableOperationId,
                traceContext: null,
                causationId: envelope.CausationId,
                partitionKey: envelope.PartitionKey,
                metadata: envelope.Metadata);
        }
    }
}
