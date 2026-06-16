using Azure.Messaging.ServiceBus;
using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bondstone.Transport.ServiceBus;

internal sealed class ServiceBusReceiveWorker(
    ServiceBusClient client,
    IServiceScopeFactory scopeFactory,
    IEnumerable<ServiceBusReceiveWorkerRegistration> registrations,
    ILogger<ServiceBusReceiveWorker> logger)
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
        ServiceBusProcessor processor =
            registration.QueueName is not null
                ? _client.CreateProcessor(
                    registration.QueueName,
                    registration.ProcessorOptions)
                : _client.CreateProcessor(
                    registration.TopicName,
                    registration.SubscriptionName,
                    registration.ProcessorOptions);

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
        IDurableEnvelopeReceiver receiver =
            scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();

        await receiver.ReceiveAsync(
            args.Message.Body.ToMemory(),
            registration.Binding,
            args.CancellationToken);
        await args.CompleteMessageAsync(
            args.Message,
            args.CancellationToken);
    }

    private Task ProcessErrorAsync(
        ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Azure Service Bus receive worker failed for entity '{EntityPath}' during '{ErrorSource}'.",
            args.EntityPath,
            args.ErrorSource);
        return Task.CompletedTask;
    }
}
