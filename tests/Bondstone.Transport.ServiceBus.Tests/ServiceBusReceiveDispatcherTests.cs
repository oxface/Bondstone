using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.ServiceBus.Inbox;
using Bondstone.Transport.ServiceBus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bondstone.Transport.ServiceBus.Tests;

public sealed class ServiceBusReceiveDispatcherTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenQueueAcceptsModule_DispatchesCommand()
    {
        var commandPipeline = new RecordingCommandReceivePipeline();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            commandPipeline,
            new RecordingEventReceivePipeline(),
            serviceBus =>
                serviceBus.ReceiveQueue("fulfillment-commands")
                    .AcceptModule("fulfillment"));
        IServiceBusReceivedMessageDispatcher dispatcher =
            serviceProvider.GetRequiredService<IServiceBusReceivedMessageDispatcher>();
        ServiceBusTransportMessage message = CreateMessage();

        await dispatcher.DispatchAsync(
            ServiceBusReceiveSource.ForQueue("fulfillment-commands"),
            message);

        Assert.Equal(1, commandPipeline.HandledCount);
        Assert.NotNull(commandPipeline.Envelope);
        Assert.Equal("fulfillment", commandPipeline.Envelope.TargetModule);
        Assert.Equal("fulfillment.order.reserve.v1", commandPipeline.Envelope.MessageTypeName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenSubscriptionHasSubscribers_DispatchesEachSubscriber()
    {
        var eventPipeline = new RecordingEventReceivePipeline();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            eventPipeline,
            serviceBus =>
                serviceBus.ReceiveSubscription("sales-events", "fulfillment-and-billing")
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "fulfillment",
                        "fulfillment.sales-order-projection.v1")
                    .SubscribeEvent(
                        "sales.order.submitted.v1",
                        "billing",
                        "billing.sales-order-projection.v1"));
        IServiceBusReceivedMessageDispatcher dispatcher =
            serviceProvider.GetRequiredService<IServiceBusReceivedMessageDispatcher>();
        ServiceBusTransportMessage message = CreateMessage(
            MessageKind.Event,
            targetModule: null,
            messageTypeName: "sales.order.submitted.v1");

        await dispatcher.DispatchAsync(
            ServiceBusReceiveSource.ForSubscription(
                "sales-events",
                "fulfillment-and-billing"),
            message);

        Assert.Equal(2, eventPipeline.Deliveries.Count);
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
    public void DescribeReceiveSource_WhenSubscriptionIsBound_ReturnsBindingDiagnostic()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());
        services.AddBondstone(
            bondstone => bondstone.Outbox.UseServiceBusTransport(
                serviceBus =>
                    serviceBus.ReceiveSubscription("sales-events", "fulfillment")
                        .SubscribeEvent(
                            "sales.order.submitted.v1",
                            "fulfillment",
                            "fulfillment.sales-order-projection.v1")));

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceBusTopologyDiagnostics diagnostics =
            serviceProvider.GetRequiredService<IServiceBusTopologyDiagnostics>();

        ServiceBusReceiveSourceDiagnostic diagnostic =
            diagnostics.DescribeReceiveSource(
                ServiceBusReceiveSource.ForSubscription("sales-events", "fulfillment"));

        Assert.True(diagnostic.HasBinding);
        Assert.Equal(ServiceBusReceiveSourceKind.Subscription, diagnostic.Source.Kind);
        Assert.Empty(diagnostic.AcceptedModules);
        Assert.Single(diagnostic.EventSubscriptions);
        Assert.Equal(
            "sales.order.submitted.v1",
            diagnostic.EventSubscriptions.Single().MessageTypeName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenReceiveSourceIsNotBound_Throws()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline(),
            _ => { });
        IServiceBusReceivedMessageDispatcher dispatcher =
            serviceProvider.GetRequiredService<IServiceBusReceivedMessageDispatcher>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync(
                ServiceBusReceiveSource.ForQueue("fulfillment-commands"),
                CreateMessage()));

        Assert.Contains("is not bound", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FromReceivedMessage_WhenMessageContainsIdentity_ReturnsTransportMessage()
    {
        ServiceBusTransportMessage source = CreateMessage();
        ServiceBusReceivedMessage receivedMessage =
            ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(source.Body),
                messageId: source.MessageId,
                partitionKey: source.PartitionKey,
                correlationId: source.CorrelationId,
                subject: source.Subject,
                properties: source.ApplicationProperties.ToDictionary(
                    static entry => entry.Key,
                    static entry => entry.Value,
                    StringComparer.Ordinal));

        ServiceBusTransportMessage mapped =
            ServiceBusReceivedMessageMapper.FromReceivedMessage(receivedMessage);

        Assert.Equal(source.Body, mapped.Body);
        Assert.Equal(source.MessageId, mapped.MessageId);
        Assert.Equal(source.Subject, mapped.Subject);
        Assert.Equal(source.CorrelationId, mapped.CorrelationId);
        Assert.Equal(source.PartitionKey, mapped.PartitionKey);
        Assert.Equal(
            MessageKind.Command.ToString(),
            mapped.ApplicationProperties[BondstoneServiceBusHeaders.MessageKind]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void FromReceivedMessage_WhenIdentityIsOnlyInApplicationProperties_ReturnsTransportMessage()
    {
        ServiceBusTransportMessage source = CreateMessage();
        ServiceBusReceivedMessage receivedMessage =
            ServiceBusModelFactory.ServiceBusReceivedMessage(
                body: BinaryData.FromString(source.Body),
                properties: new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    [BondstoneServiceBusHeaders.MessageId] = source.MessageId,
                    [BondstoneServiceBusHeaders.MessageTypeName] = source.Subject,
                });

        ServiceBusTransportMessage mapped =
            ServiceBusReceivedMessageMapper.FromReceivedMessage(receivedMessage);

        Assert.Equal(source.MessageId, mapped.MessageId);
        Assert.Equal(source.Subject, mapped.Subject);
        Assert.Equal(source.MessageId, mapped.CorrelationId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WhenDispatchSucceeds_CompletesMessage()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline(),
            serviceBus =>
                serviceBus.ReceiveQueue("fulfillment-commands")
                    .AcceptModule("fulfillment"));
        IServiceBusReceivedMessageHandler handler =
            serviceProvider.GetRequiredService<IServiceBusReceivedMessageHandler>();
        ServiceBusReceivedMessage message = CreateReceivedMessage(CreateMessage());
        ServiceBusReceivedMessage? completedMessage = null;

        await handler.HandleAsync(
            ServiceBusReceiveSource.ForQueue("fulfillment-commands"),
            message,
            (receivedMessage, _) =>
            {
                completedMessage = receivedMessage;
                return ValueTask.CompletedTask;
            });

        Assert.Same(message, completedMessage);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleAsync_WhenDispatchFails_DoesNotCompleteMessage()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new RecordingCommandReceivePipeline(),
            new RecordingEventReceivePipeline(),
            _ => { });
        IServiceBusReceivedMessageHandler handler =
            serviceProvider.GetRequiredService<IServiceBusReceivedMessageHandler>();
        bool completed = false;

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await handler.HandleAsync(
                ServiceBusReceiveSource.ForQueue("fulfillment-commands"),
                CreateReceivedMessage(CreateMessage()),
                (_, _) =>
                {
                    completed = true;
                    return ValueTask.CompletedTask;
                }));

        Assert.False(completed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseReceiveWorker_WhenConfigured_RegistersHostedService()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());

        services.AddBondstone(
            bondstone => bondstone.Outbox.UseServiceBusTransport(
                serviceBus =>
                {
                    serviceBus.ReceiveQueue("fulfillment-commands")
                        .AcceptModule("fulfillment");
                    serviceBus.UseReceiveWorker();
                }));

        ServiceDescriptor descriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType?.Name == "ServiceBusReceiveWorker");
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        ServiceBusReceiveWorkerOptions options =
            serviceProvider.GetRequiredService<IOptions<ServiceBusReceiveWorkerOptions>>().Value;
        Assert.Equal(1, options.MaxConcurrentCalls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WhenMaxConcurrentCallsIsZero_Throws()
    {
        var options = new ServiceBusReceiveWorkerOptions
        {
            MaxConcurrentCalls = 0,
        };

        Assert.Throws<ArgumentOutOfRangeException>(options.Validate);
    }

    private static ServiceProvider CreateServiceProvider(
        RecordingCommandReceivePipeline commandPipeline,
        RecordingEventReceivePipeline eventPipeline,
        Action<BondstoneServiceBusTransportBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IServiceBusMessageSender>(new RecordingServiceBusMessageSender());
        services.AddSingleton<IModuleCommandReceivePipeline>(commandPipeline);
        services.AddSingleton<IModuleEventReceivePipeline>(eventPipeline);
        services.AddBondstone(bondstone => bondstone.Outbox.UseServiceBusTransport(configure));

        return services.BuildServiceProvider();
    }

    private static ServiceBusTransportMessage CreateMessage(
        MessageKind messageKind = MessageKind.Command,
        string? targetModule = "fulfillment",
        string messageTypeName = "fulfillment.order.reserve.v1")
    {
        var envelope = new ServiceBusDurableMessageEnvelope(
            Guid.Parse("3f1a9e26-75d4-4a7d-bb48-ae453f5e5e02"),
            messageKind.ToString(),
            messageTypeName,
            "sales",
            targetModule,
            """{"orderId":"A-100"}""",
            null,
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"),
            Guid.Parse("5dac5be5-d1ef-432d-a5d5-597103ae44c9"),
            null,
            null,
            null,
            Guid.Parse("a2d07b16-258d-4ad2-b310-1ef95d5c0936"),
            "orders/A-100");
        string messageId = envelope.MessageId.ToString("D");

        return new ServiceBusTransportMessage(
            JsonSerializer.Serialize(envelope),
            messageId,
            messageTypeName,
            envelope.DurableOperationId!.Value.ToString("D"),
            envelope.PartitionKey,
            new Dictionary<string, object>(StringComparer.Ordinal)
            {
                [BondstoneServiceBusHeaders.MessageId] = messageId,
                [BondstoneServiceBusHeaders.MessageKind] = messageKind.ToString(),
                [BondstoneServiceBusHeaders.MessageTypeName] = messageTypeName,
            });
    }

    private static ServiceBusReceivedMessage CreateReceivedMessage(
        ServiceBusTransportMessage message)
    {
        return ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: BinaryData.FromString(message.Body),
            messageId: message.MessageId,
            partitionKey: message.PartitionKey,
            correlationId: message.CorrelationId,
            subject: message.Subject,
            properties: message.ApplicationProperties.ToDictionary(
                static entry => entry.Key,
                static entry => entry.Value,
                StringComparer.Ordinal));
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

    private sealed class RecordingServiceBusMessageSender : IServiceBusMessageSender
    {
        public ValueTask SendAsync(
            string entityName,
            ServiceBusTransportMessage message,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
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
}
