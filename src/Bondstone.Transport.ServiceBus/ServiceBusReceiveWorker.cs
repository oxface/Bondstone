using Azure.Messaging.ServiceBus;
using Bondstone.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bondstone.Transport.ServiceBus;

internal sealed class ServiceBusReceiveWorker(
    ServiceBusClient client,
    IServiceScopeFactory scopeFactory,
    IEnumerable<ServiceBusReceiveWorkerRegistration> registrations,
    ILogger<ServiceBusReceiveWorker> logger,
    TimeProvider? timeProvider = null)
    : BackgroundService
{
    private readonly ServiceBusClient _client =
        client ?? throw new ArgumentNullException(nameof(client));
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly IReadOnlyCollection<ServiceBusReceiveWorkerRegistration> _registrations =
        registrations?.ToArray() ?? throw new ArgumentNullException(nameof(registrations));
    private readonly ILogger<ServiceBusReceiveWorker> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (_registrations.Count == 0)
        {
            return;
        }

        ServiceBusProcessor[] processors = _registrations
            .Select(CreateProcessor)
            .ToArray();

        foreach (ServiceBusProcessor processor in processors)
        {
            await processor.StartProcessingAsync(stoppingToken);
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
            foreach (ServiceBusProcessor processor in processors)
            {
                await processor.StopProcessingAsync(CancellationToken.None);
                await processor.DisposeAsync();
            }
        }
    }

    private ServiceBusProcessor CreateProcessor(
        ServiceBusReceiveWorkerRegistration registration)
    {
        ServiceBusProcessorOptions processorOptions =
            ServiceBusReceiveWorkerOptions.CloneManualCompletionProcessorOptions(
                registration.ProcessorOptions);

        ServiceBusProcessor processor =
            registration.QueueName is not null
                ? _client.CreateProcessor(
                    registration.QueueName,
                    processorOptions)
                : _client.CreateProcessor(
                    registration.TopicName,
                    registration.SubscriptionName,
                    processorOptions);

        processor.ProcessMessageAsync += args =>
            ProcessMessageAsync(args, registration);
        processor.ProcessErrorAsync += ProcessErrorAsync;
        return processor;
    }

    private async Task ProcessMessageAsync(
        ProcessMessageEventArgs args,
        ServiceBusReceiveWorkerRegistration registration)
    {
        await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await IngestToDurableIncomingInboxAsync(
            scope.ServiceProvider,
            args.Message.Body.ToMemory(),
            registration,
            args.CancellationToken);
        await args.CompleteMessageAsync(
            args.Message,
            args.CancellationToken);
    }

    private async ValueTask IngestToDurableIncomingInboxAsync(
        IServiceProvider serviceProvider,
        ReadOnlyMemory<byte> body,
        ServiceBusReceiveWorkerRegistration registration,
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
        ServiceBusReceiveWorkerRegistration registration)
    {
        DurableIncomingInboxKey key = envelope.MessageKind switch
        {
            MessageKind.Command => ResolveCommandKey(serviceProvider, envelope),
            MessageKind.Event => ResolveEventKey(serviceProvider, envelope, registration),
            _ => throw new NotSupportedException(
                $"Azure Service Bus durable incoming inbox ingestion does not support message kind '{envelope.MessageKind}'."),
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
        ServiceBusReceiveWorkerRegistration registration)
    {
        DurableEnvelopeReceiveBinding binding = registration.Binding
            ?? throw new BondstoneSetupArgumentException(
                BondstoneSetupCodes.MissingReceiveBinding,
                "Azure Service Bus durable incoming inbox event ingestion requires subscriber module and subscriber identity binding.",
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
                "Azure Service Bus durable incoming inbox event ingestion requires subscriber module and subscriber identity binding.",
                nameof(binding));
        }

        return new DurableEnvelopeReceiveBinding(
            binding.SubscriberModule.Trim(),
            binding.SubscriberIdentity.Trim());
    }

    private Task ProcessErrorAsync(
        ProcessErrorEventArgs args)
    {
        _logger.LogError(
            ServiceBusReceiveWorkerLogEvents.ReceiveFailed,
            args.Exception,
            "Azure Service Bus receive worker failed for entity '{EntityPath}' during '{ErrorSource}'.",
            args.EntityPath,
            args.ErrorSource);
        return Task.CompletedTask;
    }
}
