using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableEnvelopeReceiverTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveCommandAsync_WhenEnvelopeIsCommand_UsesCommandPipeline()
    {
        var commandPipeline = new RecordingCommandReceivePipeline();
        var eventPipeline = new RecordingEventReceivePipeline();
        using ServiceProvider serviceProvider = CreateServiceProvider(
            commandPipeline,
            eventPipeline);
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableEnvelopeReceiver receiver =
            scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();
        DurableMessageEnvelope envelope = CreateCommandEnvelope();

        DurableInboxHandleResult result = await receiver.ReceiveCommandAsync(envelope);

        Assert.Same(commandPipeline.Result, result);
        Assert.Same(envelope, commandPipeline.Envelope);
        Assert.Null(eventPipeline.Envelope);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveEventAsync_WhenEnvelopeIsEvent_UsesEventPipeline()
    {
        var commandPipeline = new RecordingCommandReceivePipeline();
        var eventPipeline = new RecordingEventReceivePipeline();
        using ServiceProvider serviceProvider = CreateServiceProvider(
            commandPipeline,
            eventPipeline);
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableEnvelopeReceiver receiver =
            scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();
        DurableMessageEnvelope envelope = CreateEventEnvelope();

        DurableInboxHandleResult result = await receiver.ReceiveEventAsync(
            envelope,
            "billing",
            "billing.order-placed.v1");

        Assert.Same(eventPipeline.Result, result);
        Assert.Same(envelope, eventPipeline.Envelope);
        Assert.Equal("billing", eventPipeline.SubscriberModule);
        Assert.Equal("billing.order-placed.v1", eventPipeline.SubscriberIdentity);
        Assert.Null(commandPipeline.Envelope);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenEnvelopeIsCommand_UsesCommandPipeline()
    {
        var commandPipeline = new RecordingCommandReceivePipeline();
        var eventPipeline = new RecordingEventReceivePipeline();
        using ServiceProvider serviceProvider = CreateServiceProvider(
            commandPipeline,
            eventPipeline);
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableEnvelopeReceiver receiver =
            scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();
        DurableMessageEnvelope envelope = CreateCommandEnvelope();

        DurableInboxHandleResult result = await receiver.ReceiveAsync(envelope);

        Assert.Same(commandPipeline.Result, result);
        Assert.Same(envelope, commandPipeline.Envelope);
        Assert.Null(eventPipeline.Envelope);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenUtf8EnvelopeIsEvent_UsesEventPipeline()
    {
        var commandPipeline = new RecordingCommandReceivePipeline();
        var eventPipeline = new RecordingEventReceivePipeline();
        using ServiceProvider serviceProvider = CreateServiceProvider(
            commandPipeline,
            eventPipeline);
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableEnvelopeReceiver receiver =
            scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();
        IDurableMessageEnvelopeSerializer serializer =
            scope.ServiceProvider.GetRequiredService<IDurableMessageEnvelopeSerializer>();
        DurableMessageEnvelope envelope = CreateEventEnvelope();

        DurableInboxHandleResult result = await receiver.ReceiveAsync(
            serializer.SerializeToUtf8Bytes(envelope),
            new DurableEnvelopeReceiveBinding("billing", "billing.order-placed.v1"));

        Assert.Same(eventPipeline.Result, result);
        Assert.Equal(envelope, eventPipeline.Envelope);
        Assert.Equal("billing", eventPipeline.SubscriberModule);
        Assert.Equal("billing.order-placed.v1", eventPipeline.SubscriberIdentity);
        Assert.Null(commandPipeline.Envelope);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveAsync_WhenEnvelopeIsEventWithoutBinding_Throws()
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline());
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableEnvelopeReceiver receiver =
            scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await receiver.ReceiveAsync(CreateEventEnvelope()));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveCommandAsync_WhenEnvelopeIsEvent_Throws()
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline());
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableEnvelopeReceiver receiver =
            scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();

        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await receiver.ReceiveCommandAsync(CreateEventEnvelope()));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReceiveEventAsync_WhenEnvelopeIsCommand_Throws()
    {
        using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline());
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableEnvelopeReceiver receiver =
            scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();

        await Assert.ThrowsAsync<NotSupportedException>(
            async () => await receiver.ReceiveEventAsync(
                CreateCommandEnvelope(),
                "billing",
                "billing.order-placed.v1"));
    }

    private static ServiceProvider CreateServiceProvider(
        IModuleCommandReceivePipeline commandPipeline,
        IModuleEventReceivePipeline eventPipeline)
    {
        var services = new ServiceCollection();
        services.AddSingleton(commandPipeline);
        services.AddSingleton(eventPipeline);
        services.AddBondstone(_ => { });
        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    private static DurableMessageEnvelope CreateCommandEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "fulfillment.inventory.reserve.v1",
            "ordering",
            "fulfillment",
            "{}",
            DateTimeOffset.Parse("2026-06-16T00:00:00+00:00"));
    }

    private static DurableMessageEnvelope CreateEventEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            MessageKind.Event,
            "ordering.order-placed.v1",
            "ordering",
            targetModule: null,
            payload: "{}",
            DateTimeOffset.Parse("2026-06-16T00:00:00+00:00"));
    }

    private sealed class RecordingCommandReceivePipeline : IModuleCommandReceivePipeline
    {
        public DurableMessageEnvelope? Envelope { get; private set; }

        public DurableInboxHandleResult Result { get; } =
            CreateResult("fulfillment", "fulfillment.inventory.reserve.v1");

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            Envelope = envelope;
            return ValueTask.FromResult(Result);
        }
    }

    private sealed class RecordingEventReceivePipeline : IModuleEventReceivePipeline
    {
        public DurableMessageEnvelope? Envelope { get; private set; }

        public string? SubscriberModule { get; private set; }

        public string? SubscriberIdentity { get; private set; }

        public DurableInboxHandleResult Result { get; } =
            CreateResult("billing", "billing.order-placed.v1");

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableMessageEnvelope envelope,
            string subscriberModule,
            string subscriberIdentity,
            CancellationToken ct = default)
        {
            Envelope = envelope;
            SubscriberModule = subscriberModule;
            SubscriberIdentity = subscriberIdentity;
            return ValueTask.FromResult(Result);
        }
    }

    private static DurableInboxHandleResult CreateResult(
        string moduleName,
        string handlerIdentity)
    {
        return new DurableInboxHandleResult(
            DurableInboxHandleStatus.Handled,
            new DurableInboxRecord(
                new DurableInboxMessageKey(
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    moduleName,
                    handlerIdentity),
                DateTimeOffset.Parse("2026-06-16T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-16T00:00:01+00:00")));
    }
}
