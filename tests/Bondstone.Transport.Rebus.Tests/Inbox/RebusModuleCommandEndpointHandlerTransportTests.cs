using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Serialization.Json;
using Rebus.Transport.InMem;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Inbox;

public sealed class RebusModuleCommandEndpointHandlerTransportTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendLocal_WhenModuleCommandEndpointHandlerIsBound_DispatchesToModuleCommandExecutor()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled);
        var commandSignal = new ReceivedCommandSignal();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            inboxExecutor,
            commandSignal);

        using var activator = new BuiltinHandlerActivator();
        activator.Handle<RebusDurableMessageEnvelope>(
            async envelope =>
            {
                using IServiceScope scope = serviceProvider.CreateScope();
                RebusModuleCommandEndpointHandler handler =
                    scope.ServiceProvider.GetRequiredService<RebusModuleCommandEndpointHandler>();

                await handler.Handle(envelope);
            });

        var network = new InMemNetwork();
        using IBus bus = StartBus(activator, network, "fulfillment-commands");

        await bus.SendLocal(CreateEnvelope());

        ReserveOrderCommand handledCommand = await WaitAsync(commandSignal.Task);

        Assert.Equal("A-100", handledCommand.OrderId);
        Assert.NotNull(inboxExecutor.Record);
        Assert.Equal(CreateEnvelope().MessageId, inboxExecutor.Record.Key.MessageId);
        Assert.Equal("fulfillment", inboxExecutor.Record.Key.ModuleName);
        Assert.Equal("fulfillment.order.reserve.v1", inboxExecutor.Record.Key.HandlerIdentity);
        Assert.Equal(1, inboxExecutor.HandlerCalls);
        Assert.Equal(1, inboxExecutor.CommitCalls);
        Assert.Equal(0, network.Count("fulfillment-commands"));
        Assert.Equal(0, network.Count("error"));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SendLocal_WhenEndpointBindingIsInconsistent_MovesMessageToErrorQueue()
    {
        var failed = new TaskCompletionSource<Exception>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled),
            new ReceivedCommandSignal(),
            boundEndpointName: "billing-commands");

        using var activator = new BuiltinHandlerActivator();
        activator.Handle<RebusDurableMessageEnvelope>(
            async envelope =>
            {
                try
                {
                    using IServiceScope scope = serviceProvider.CreateScope();
                    RebusModuleCommandEndpointHandler handler =
                        scope.ServiceProvider.GetRequiredService<RebusModuleCommandEndpointHandler>();

                    await handler.Handle(envelope);
                }
                catch (Exception exception)
                {
                    failed.TrySetResult(exception);
                    throw;
                }
            });

        var network = new InMemNetwork();
        using IBus bus = StartBus(activator, network, "fulfillment-commands");

        await bus.SendLocal(CreateEnvelope());

        Exception exception = await WaitAsync(failed.Task);
        await WaitUntilAsync(() => network.Count("error") == 1);

        Assert.IsType<InvalidOperationException>(exception);
        Assert.Contains("billing-commands", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, network.Count("fulfillment-commands"));
    }

    private static ServiceProvider CreateServiceProvider(
        CapturingInboxHandlerExecutor inboxExecutor,
        ReceivedCommandSignal commandSignal,
        string boundEndpointName = "fulfillment-commands")
    {
        var services = new ServiceCollection();

        services.AddSingleton(commandSignal);
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);
        services.AddBondstoneRebusModuleCommandEndpointHandler(boundEndpointName);
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
            if (boundEndpointName != "fulfillment-commands")
            {
                bondstone.Module("billing", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                    module.Commands.RegisterHandler<RecordBillingCommand, RecordBillingHandler>();
                });
            }

            bondstone.UseRebusTransport(rebus =>
            {
                rebus.ReceiveEndpoint("fulfillment-commands").AcceptModule("fulfillment");
                if (boundEndpointName != "fulfillment-commands")
                {
                    rebus.ReceiveEndpoint(boundEndpointName).AcceptModule("billing");
                }
            });
        });

        return services.BuildServiceProvider();
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
            throw new TimeoutException("Timed out waiting for Rebus module command endpoint handler signal.");
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
            DateTimeOffset.Parse("2026-06-08T12:00:00+00:00"),
            DurableOperationId: null,
            TraceParent: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
            TraceState: null,
            TraceBaggage: null,
            CausationId: null,
            PartitionKey: "orders/A-100");
    }

    [DurableCommandIdentity("fulfillment.order.reserve.v1")]
    public sealed record ReserveOrderCommand(string OrderId) : IDurableCommand;

    [DurableCommandIdentity("billing.record.v1")]
    public sealed record RecordBillingCommand(string OrderId) : IDurableCommand;

    public sealed class ReserveOrderHandler(ReceivedCommandSignal signal)
        : ICommandHandler<ReserveOrderCommand>
    {
        public ValueTask HandleAsync(
            ReserveOrderCommand command,
            CancellationToken ct = default)
        {
            signal.SetResult(command);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class RecordBillingHandler : ICommandHandler<RecordBillingCommand>
    {
        public ValueTask HandleAsync(
            RecordBillingCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ReceivedCommandSignal
    {
        private readonly TaskCompletionSource<ReserveOrderCommand> _source =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ReserveOrderCommand> Task => _source.Task;

        public void SetResult(ReserveOrderCommand command)
        {
            _source.TrySetResult(command);
        }
    }

    private sealed class CapturingInboxHandlerExecutor(DurableInboxHandleStatus status)
        : IDurableInboxHandlerExecutor
    {
        public DurableInboxRecord? Record { get; private set; }

        public int HandlerCalls { get; private set; }

        public int CommitCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            Func<CancellationToken, ValueTask> commit,
            CancellationToken ct = default)
        {
            Record = record;

            if (status == DurableInboxHandleStatus.Handled)
            {
                HandlerCalls++;
                await handler(ct);
                CommitCalls++;
                await commit(ct);
            }

            return new DurableInboxHandleResult(status, record);
        }
    }
}
