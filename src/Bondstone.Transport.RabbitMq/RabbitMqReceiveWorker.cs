using Bondstone.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Bondstone.Transport.RabbitMq;

internal sealed class RabbitMqReceiveWorker(
    IChannel channel,
    IServiceScopeFactory scopeFactory,
    IEnumerable<RabbitMqReceiveWorkerRegistration> registrations,
    ILogger<RabbitMqReceiveWorker> logger,
    TimeProvider? timeProvider = null)
    : BackgroundService
{
    private readonly IChannel _channel =
        channel ?? throw new ArgumentNullException(nameof(channel));
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly IReadOnlyCollection<RabbitMqReceiveWorkerRegistration> _registrations =
        registrations?.ToArray() ?? throw new ArgumentNullException(nameof(registrations));
    private readonly ILogger<RabbitMqReceiveWorker> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (_registrations.Count == 0)
        {
            return;
        }

        var consumerTags = new List<string>();
        foreach (RabbitMqReceiveWorkerRegistration registration in _registrations)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (_, args) =>
                ReceiveAsync(args, registration, stoppingToken);

            string consumerTag = await _channel.BasicConsumeAsync(
                queue: registration.QueueName,
                autoAck: false,
                consumerTag: registration.ConsumerTag ?? string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer,
                cancellationToken: stoppingToken);
            consumerTags.Add(consumerTag);
        }

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            foreach (string consumerTag in consumerTags)
            {
                await _channel.BasicCancelAsync(
                    consumerTag,
                    noWait: false,
                    cancellationToken: CancellationToken.None);
            }
        }
    }

    private async Task ReceiveAsync(
        BasicDeliverEventArgs args,
        RabbitMqReceiveWorkerRegistration registration,
        CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            if (registration.ReceiveMode == RabbitMqReceiveWorkerMode.DurableIncomingInboxIngestion)
            {
                await IngestToDurableIncomingInboxAsync(
                    scope.ServiceProvider,
                    args.Body,
                    registration,
                    stoppingToken);
            }
            else
            {
                IDurableEnvelopeReceiver receiver =
                    scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();

                await receiver.ReceiveAsync(
                    args.Body,
                    registration.Binding,
                    stoppingToken);
            }

            await _channel.BasicAckAsync(
                args.DeliveryTag,
                multiple: false,
                cancellationToken: stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                RabbitMqReceiveWorkerLogEvents.ReceiveFailed,
                ex,
                "RabbitMQ receive worker failed for queue '{QueueName}'.",
                registration.QueueName);

            await _channel.BasicNackAsync(
                args.DeliveryTag,
                multiple: false,
                requeue: registration.RequeueOnFailure,
                cancellationToken: CancellationToken.None);
        }
    }

    private async ValueTask IngestToDurableIncomingInboxAsync(
        IServiceProvider serviceProvider,
        ReadOnlyMemory<byte> body,
        RabbitMqReceiveWorkerRegistration registration,
        CancellationToken ct)
    {
        IDurableMessageEnvelopeSerializer serializer =
            serviceProvider.GetRequiredService<IDurableMessageEnvelopeSerializer>();
        DurableMessageEnvelope envelope = serializer.Deserialize(body);
        DurableIncomingInboxRecord record = CreateIncomingInboxRecord(
            serviceProvider,
            envelope,
            registration);

        IDurableIncomingInboxIngestionBoundaryResolver boundaryResolver =
            serviceProvider.GetRequiredService<IDurableIncomingInboxIngestionBoundaryResolver>();
        DurableIncomingInboxIngestionBoundary boundary =
            boundaryResolver.Resolve(record.ReceiverModule);

        await boundary.IngestAndSaveAsync(record, ct);
    }

    private DurableIncomingInboxRecord CreateIncomingInboxRecord(
        IServiceProvider serviceProvider,
        DurableMessageEnvelope envelope,
        RabbitMqReceiveWorkerRegistration registration)
    {
        DurableIncomingInboxKey key = envelope.MessageKind switch
        {
            MessageKind.Command => ResolveCommandKey(serviceProvider, envelope),
            MessageKind.Event => ResolveEventKey(serviceProvider, envelope, registration),
            _ => throw new NotSupportedException(
                $"RabbitMQ durable incoming inbox ingestion does not support message kind '{envelope.MessageKind}'."),
        };

        return new DurableIncomingInboxRecord(
            key,
            envelope,
            _timeProvider.GetUtcNow(),
            sourceTransportName: registration.SourceTransportName);
    }

    private static DurableIncomingInboxKey ResolveCommandKey(
        IServiceProvider serviceProvider,
        DurableMessageEnvelope envelope)
    {
        IModuleCommandRouteRegistry routeRegistry =
            serviceProvider.GetRequiredService<IModuleCommandRouteRegistry>();
        ModuleCommandRoute route = routeRegistry.GetByMessageTypeName(
            envelope.TargetModule!,
            envelope.MessageTypeName);
        string handlerIdentity = route.HandlerIdentity
            ?? throw new InvalidOperationException(
                $"Durable command route for message type '{envelope.MessageTypeName}' does not have a handler identity.");

        return DurableIncomingInboxKey.ForCommandHandler(
            envelope.MessageId,
            route.ModuleName,
            handlerIdentity);
    }

    private static DurableIncomingInboxKey ResolveEventKey(
        IServiceProvider serviceProvider,
        DurableMessageEnvelope envelope,
        RabbitMqReceiveWorkerRegistration registration)
    {
        DurableEnvelopeReceiveBinding binding = registration.Binding
            ?? throw new BondstoneSetupArgumentException(
                BondstoneSetupCodes.MissingReceiveBinding,
                "RabbitMQ durable incoming inbox event ingestion requires subscriber module and subscriber identity binding.",
                nameof(registration));
        binding = NormalizeReceiveBinding(binding);

        IModuleEventSubscriberRegistry subscriberRegistry =
            serviceProvider.GetRequiredService<IModuleEventSubscriberRegistry>();
        ModuleEventSubscriberRegistration subscriber;
        try
        {
            subscriber = subscriberRegistry.GetSubscriber(
                binding.SubscriberModule,
                envelope.MessageTypeName,
                binding.SubscriberIdentity);
        }
        catch (InvalidOperationException exception)
        {
            throw new BondstoneSetupException(
                BondstoneSetupCodes.MissingReceiveBinding,
                exception.Message,
                exception);
        }

        return DurableIncomingInboxKey.ForEventSubscriber(
            envelope.MessageId,
            subscriber.ModuleName,
            subscriber.SubscriberIdentity);
    }

    private static DurableEnvelopeReceiveBinding NormalizeReceiveBinding(
        DurableEnvelopeReceiveBinding binding)
    {
        if (string.IsNullOrWhiteSpace(binding.SubscriberModule)
            || string.IsNullOrWhiteSpace(binding.SubscriberIdentity))
        {
            throw new BondstoneSetupArgumentException(
                BondstoneSetupCodes.MissingReceiveBinding,
                "RabbitMQ durable incoming inbox event ingestion requires subscriber module and subscriber identity binding.",
                nameof(binding));
        }

        return new DurableEnvelopeReceiveBinding(
            binding.SubscriberModule.Trim(),
            binding.SubscriberIdentity.Trim());
    }
}
