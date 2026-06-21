using Bondstone.Configuration;
using Bondstone.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Local.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.Local.Tests;

public sealed class LocalDurableEnvelopeDispatcherTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenCommandQueueBindingExists_DispatchesCommandThroughReceivePipeline()
    {
        var commandPipeline = new RecordingCommandReceivePipeline();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            commandPipeline,
            new RecordingEventReceivePipeline(),
            local =>
            {
                local.RouteModule("fulfillment").ToQueue("fulfillment.commands");
                local.Queue("fulfillment.commands").AcceptModule("fulfillment");
            });
        IDurableEnvelopeDispatcher dispatcher =
            serviceProvider.GetRequiredService<IDurableEnvelopeDispatcher>();
        DurableOutboxRecord record = CreateRecord();

        await dispatcher.DispatchAsync(record);

        Assert.Same(record.Envelope, commandPipeline.Envelope);
        Assert.Equal(1, commandPipeline.HandledCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenModuleQueueConventionIsConfigured_DispatchesCommandThroughReceivePipeline()
    {
        var commandPipeline = new RecordingCommandReceivePipeline();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            commandPipeline,
            new RecordingEventReceivePipeline(),
            local => local.UseModuleQueueConvention());
        IDurableEnvelopeDispatcher dispatcher =
            serviceProvider.GetRequiredService<IDurableEnvelopeDispatcher>();
        DurableOutboxRecord record = CreateRecord();

        await dispatcher.DispatchAsync(record);

        Assert.Same(record.Envelope, commandPipeline.Envelope);
        Assert.Equal(1, commandPipeline.HandledCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenEventQueueHasSubscribers_DispatchesEventToEachSubscriber()
    {
        var eventPipeline = new RecordingEventReceivePipeline();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            eventPipeline,
            local =>
            {
                local.RouteEvent("sales.order.submitted.v1").ToQueue("sales-events");
                local.Queue("sales-events")
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "fulfillment",
                        "fulfillment.sales-order-projection.v1")
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "billing",
                        "billing.sales-order-projection.v1");
            });
        IDurableEnvelopeDispatcher dispatcher =
            serviceProvider.GetRequiredService<IDurableEnvelopeDispatcher>();
        DurableOutboxRecord record = CreateRecord(
            MessageKind.Event,
            targetModule: null,
            messageTypeName: "sales.order.submitted.v1");

        await dispatcher.DispatchAsync(record);

        Assert.Equal(2, eventPipeline.Deliveries.Count);
        Assert.All(
            eventPipeline.Deliveries,
            delivery => Assert.Same(record.Envelope, delivery.Envelope));
        Assert.Contains(
            eventPipeline.Deliveries,
            delivery =>
                delivery.SubscriberModule == "fulfillment"
                && delivery.SubscriberIdentity == "fulfillment.sales-order-projection.v1");
        Assert.Contains(
            eventPipeline.Deliveries,
            delivery =>
                delivery.SubscriberModule == "billing"
                && delivery.SubscriberIdentity == "billing.sales-order-projection.v1");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenNoQueueBindingExists_ThrowsNoTransportRoute()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline(),
            local => local.RouteModule("fulfillment").ToQueue("fulfillment.commands"));
        IDurableEnvelopeDispatcher dispatcher =
            serviceProvider.GetRequiredService<IDurableEnvelopeDispatcher>();

        InvalidOperationException exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync(CreateRecord()));

        Assert.Equal(
            BondstoneSetupCodes.MissingDispatcher,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
        Assert.Contains("No durable envelope dispatch route", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenLocalCommandRouteHasNoQueueBinding_ThrowsSetupCode()
    {
        var route = new LocalDurableEnvelopeDispatchRoute(
            CreateEmptyTopology(),
            new ThrowingDurableEnvelopeReceiver());

        InvalidOperationException exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(
            async () => await route.DispatchAsync(CreateRecord()));

        Assert.Equal(
            BondstoneSetupCodes.MissingReceiveBinding,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenLocalEventRouteHasNoSubscriberBinding_ThrowsSetupCode()
    {
        var route = new LocalDurableEnvelopeDispatchRoute(
            CreateEmptyTopology(),
            new ThrowingDurableEnvelopeReceiver());

        InvalidOperationException exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(
            async () => await route.DispatchAsync(CreateRecord(
                MessageKind.Event,
                targetModule: null,
                messageTypeName: "sales.order.submitted.v1")));

        Assert.Equal(
            BondstoneSetupCodes.MissingReceiveBinding,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDurableCommandHandlerUsesModuleQueueConvention_AllowsStartupValidation()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            RegisterFulfillmentCommandModule(bondstone);
            bondstone.UseLocalTransport(local => local.UseModuleQueueConvention());
        });
    }

    private static ServiceProvider CreateServiceProvider(
        RecordingCommandReceivePipeline commandPipeline,
        RecordingEventReceivePipeline eventPipeline,
        Action<BondstoneLocalTransportBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IModuleCommandReceivePipeline>(commandPipeline);
        services.AddSingleton<IModuleEventReceivePipeline>(eventPipeline);
        services.AddBondstone(bondstone => bondstone.UseLocalTransport(configure));

        return services.BuildServiceProvider();
    }

    private static DurableOutboxRecord CreateRecord(
        MessageKind messageKind = MessageKind.Command,
        string? targetModule = "fulfillment",
        string messageTypeName = "fulfillment.order.reserve.v1")
    {
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("3f1a9e26-75d4-4a7d-bb48-ae453f5e5e02"),
            messageKind,
            messageTypeName,
            "sales",
            targetModule,
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"));

        return new DurableOutboxRecord(
            envelope,
            DateTimeOffset.Parse("2026-06-09T12:00:01+00:00"),
            new DurableOutboxDispatchState(
                DurableOutboxStatus.Processing,
                attemptCount: 1,
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-09T12:05:00+00:00")));
    }

    private static void RegisterFulfillmentCommandModule(
        BondstoneBuilder bondstone)
    {
        bondstone.Module("fulfillment", module =>
        {
            module.UseDurableMessaging();
            module.UsePersistence("test persistence");
            module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
        });
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
                DateTimeOffset.Parse("2026-06-09T12:00:02+00:00"),
                DateTimeOffset.Parse("2026-06-09T12:00:03+00:00")));
    }

    private static LocalTransportTopology CreateEmptyTopology()
    {
        return new LocalTransportTopology(
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, LocalQueueRegistration>(StringComparer.Ordinal),
            moduleQueueNameConvention: null,
            eventQueueNameConvention: null);
    }

    private sealed class RecordingCommandReceivePipeline : IModuleCommandReceivePipeline
    {
        public DurableMessageEnvelope? Envelope { get; private set; }

        public int HandledCount { get; private set; }

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            Envelope = envelope;
            HandledCount++;

            return ValueTask.FromResult(
                CreateHandledResult(
                    envelope,
                    envelope.TargetModule!,
                    envelope.MessageTypeName));
        }
    }

    private sealed class RecordingEventReceivePipeline : IModuleEventReceivePipeline
    {
        public List<EventDelivery> Deliveries { get; } = [];

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            Deliveries.Add(new EventDelivery(
                envelope,
                subscriberModule,
                subscriberIdentity));

            return ValueTask.FromResult(
                CreateHandledResult(
                    envelope,
                    subscriberModule,
                    subscriberIdentity));
        }
    }

    private sealed record EventDelivery(
        DurableMessageEnvelope Envelope,
        string SubscriberModule,
        string SubscriberIdentity);

    private sealed class ThrowingDurableEnvelopeReceiver : IDurableEnvelopeReceiver
    {
        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            DurableMessageEnvelope envelope,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            ReadOnlyMemory<byte> utf8Json,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<DurableInboxHandleResult> ReceiveAsync(
            string json,
            DurableEnvelopeReceiveBinding? binding = null,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<DurableInboxHandleResult> ReceiveCommandAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask<DurableInboxHandleResult> ReceiveEventAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    [DurableCommandIdentity("fulfillment.test.command.v1")]
    private sealed record TestCommand : IDurableCommand;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public ValueTask HandleAsync(
            TestCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
