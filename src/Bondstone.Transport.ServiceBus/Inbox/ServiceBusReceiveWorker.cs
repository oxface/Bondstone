using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bondstone.Transport.ServiceBus.Inbox;

internal sealed class ServiceBusReceiveWorker(
    ServiceBusClient client,
    IServiceScopeFactory serviceScopeFactory,
    ServiceBusReceiveTopology receiveTopology,
    IOptions<ServiceBusReceiveWorkerOptions> options,
    ILogger<ServiceBusReceiveWorker> logger)
    : BackgroundService
{
    private readonly ServiceBusClient _client =
        client ?? throw new ArgumentNullException(nameof(client));
    private readonly IServiceScopeFactory _serviceScopeFactory =
        serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly ServiceBusReceiveTopology _receiveTopology =
        receiveTopology ?? throw new ArgumentNullException(nameof(receiveTopology));
    private readonly ServiceBusReceiveWorkerOptions _options = GetValidatedOptions(options);
    private readonly ILogger<ServiceBusReceiveWorker> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public override Task StartAsync(
        CancellationToken ct)
    {
        if (_receiveTopology.Sources.Count == 0)
        {
            throw new InvalidOperationException(
                "Service Bus receive worker requires at least one receive source binding.");
        }

        return base.StartAsync(ct);
    }

    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        List<ServiceBusProcessor> processors =
            _receiveTopology.Sources
                .Select(CreateProcessor)
                .ToList();

        try
        {
            foreach (ServiceBusProcessor processor in processors)
            {
                await processor.StartProcessingAsync(ct);
            }

            await WaitUntilStoppedAsync(ct);
        }
        finally
        {
            foreach (ServiceBusProcessor processor in processors)
            {
                await StopAndDisposeProcessorAsync(processor, ct);
            }
        }
    }

    private ServiceBusProcessor CreateProcessor(
        ServiceBusReceiveSource source)
    {
        var processorOptions = new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = _options.MaxConcurrentCalls,
            MaxAutoLockRenewalDuration = _options.MaxAutoLockRenewalDuration,
        };
        ServiceBusProcessor processor = source.Kind == ServiceBusReceiveSourceKind.Queue
            ? _client.CreateProcessor(source.EntityName, processorOptions)
            : _client.CreateProcessor(
                source.EntityName,
                source.SubscriptionName!,
                processorOptions);

        processor.ProcessMessageAsync += args => HandleMessageAsync(source, args);
        processor.ProcessErrorAsync += HandleErrorAsync;

        return processor;
    }

    private async Task HandleMessageAsync(
        ServiceBusReceiveSource source,
        ProcessMessageEventArgs args)
    {
        try
        {
            await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
            IServiceBusReceivedMessageHandler handler =
                scope.ServiceProvider.GetRequiredService<IServiceBusReceivedMessageHandler>();

            await handler.HandleAsync(
                source,
                args.Message,
                (message, completeCt) =>
                    new ValueTask(args.CompleteMessageAsync(message, completeCt)),
                args.CancellationToken);
        }
        catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Service Bus receive worker failed while handling message {MessageId} from {ReceiveSource}.",
                args.Message.MessageId,
                source.DisplayName);

            await args.AbandonMessageAsync(
                args.Message,
                cancellationToken: args.CancellationToken);
        }
    }

    private Task HandleErrorAsync(
        ProcessErrorEventArgs args)
    {
        _logger.LogError(
            args.Exception,
            "Service Bus receive worker processor error from entity {EntityPath}.",
            args.EntityPath);

        return Task.CompletedTask;
    }

    private static async Task StopAndDisposeProcessorAsync(
        ServiceBusProcessor processor,
        CancellationToken ct)
    {
        try
        {
            await processor.StopProcessingAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }

        await processor.DisposeAsync();
    }

    private static async Task WaitUntilStoppedAsync(
        CancellationToken ct)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private static ServiceBusReceiveWorkerOptions GetValidatedOptions(
        IOptions<ServiceBusReceiveWorkerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ServiceBusReceiveWorkerOptions receiveOptions = options.Value;
        receiveOptions.Validate();
        return receiveOptions;
    }
}
