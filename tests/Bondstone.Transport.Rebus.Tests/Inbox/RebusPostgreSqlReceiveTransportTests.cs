using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Npgsql;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Serialization.Json;
using Testcontainers.PostgreSql;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Inbox;

public sealed class RebusPostgreSqlReceiveTransportTests(
    RebusPostgreSqlReceiveTransportTests.PostgreSqlRebusFixture fixture)
    : IClassFixture<RebusPostgreSqlReceiveTransportTests.PostgreSqlRebusFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendLocal_WhenTypedCommandIsReceived_HandlesAndAcknowledgesPostgreSqlTransportMessage()
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
                        "postgres-receive-handler",
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

        const string tableName = "bondstone_rebus_receive_success";
        const string inputQueueName = "bondstone-rebus-postgres-receive";
        const string errorQueueName = "bondstone-rebus-postgres-error";
        using IBus bus = StartBus(
            activator,
            fixture.ConnectionString,
            tableName,
            inputQueueName,
            errorQueueName);

        await bus.SendLocal(CreateEnvelope());

        HandledCommandResult handledResult = await WaitAsync(handled.Task);
        await WaitUntilAsync(
            async () => await GetQueueCountAsync(tableName, inputQueueName) == 0);

        Assert.Equal("A-100", handledResult.Command.OrderId);
        Assert.Equal(DurableInboxHandleStatus.Handled, handledResult.Result.Status);
        Assert.Equal(1, handledResult.CommitCalls);
        Assert.Equal(CreateEnvelope().MessageId, inboxExecutor.Envelope?.MessageId);
        Assert.Equal("postgres-receive-handler", inboxExecutor.HandlerIdentity);
        Assert.Equal(1, inboxExecutor.HandlerCalls);
        Assert.Equal(1, inboxExecutor.CommitCalls);
        Assert.Equal(0, await GetQueueCountAsync(tableName, inputQueueName));
        Assert.Equal(0, await GetQueueCountAsync(tableName, errorQueueName));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendLocal_WhenTypedPipelineFails_MovesPostgreSqlTransportMessageToErrorQueue()
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
                        "postgres-receive-handler",
                        (_, _) => ValueTask.CompletedTask,
                        _ => ValueTask.CompletedTask);
                }
                catch (Exception exception)
                {
                    failed.TrySetResult(exception);
                    throw;
                }
            });

        const string tableName = "bondstone_rebus_receive_failure";
        const string inputQueueName = "bondstone-rebus-postgres-receive-failure";
        const string errorQueueName = "bondstone-rebus-postgres-error-failure";
        using IBus bus = StartBus(
            activator,
            fixture.ConnectionString,
            tableName,
            inputQueueName,
            errorQueueName);

        await bus.SendLocal(CreateEnvelope());

        Exception exception = await WaitAsync(failed.Task);
        await WaitUntilAsync(
            async () => await GetQueueCountAsync(tableName, errorQueueName) == 1);

        Assert.IsType<KeyNotFoundException>(exception);
        Assert.Null(inboxExecutor.Envelope);
        Assert.Equal(0, await GetQueueCountAsync(tableName, inputQueueName));
        Assert.Equal(1, await GetQueueCountAsync(tableName, errorQueueName));
    }

    private static IBus StartBus(
        BuiltinHandlerActivator activator,
        string connectionString,
        string tableName,
        string inputQueueName,
        string errorQueueName)
    {
        return Configure
            .With(activator)
            .Transport(transport => transport.UsePostgreSql(
                connectionString,
                tableName,
                inputQueueName,
                null,
                "public"))
            .Serialization(serializer => serializer.UseSystemTextJson())
            .Options(options => options.RetryStrategy(errorQueueName, maxDeliveryAttempts: 1))
            .Start();
    }

    private async Task<int> GetQueueCountAsync(
        string tableName,
        string queueName)
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            select count(*)
            from public."{tableName}"
            where recipient = @recipient
            """;
        command.Parameters.AddWithValue("recipient", queueName);

        object? result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static async Task<T> WaitAsync<T>(Task<T> task)
    {
        Task completed = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
        if (completed != task)
        {
            throw new TimeoutException("Timed out waiting for Rebus PostgreSQL receive test signal.");
        }

        return await task;
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        while (!await condition())
        {
            if (timeout.IsCancellationRequested)
            {
                throw new TimeoutException("Timed out waiting for Rebus PostgreSQL transport state.");
            }

            await Task.Delay(50);
        }
    }

    private static RebusDurableMessageEnvelope CreateEnvelope()
    {
        return new RebusDurableMessageEnvelope(
            Guid.Parse("8e5a3176-05f3-4022-8a99-f8db3ea5f3f8"),
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

    public sealed class PostgreSqlRebusFixture : IAsyncLifetime
    {
        private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("bondstone_rebus_tests")
            .Build();

        public string ConnectionString => $"{_container.GetConnectionString()};Pooling=false";

        public Task InitializeAsync()
        {
            return _container.StartAsync();
        }

        public Task DisposeAsync()
        {
            return _container.DisposeAsync().AsTask();
        }
    }

    private sealed record HandledCommandResult(
        ReserveOrderCommand Command,
        DurableInboxHandleResult Result,
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
