using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bondstone.Hosting.Outbox;

public sealed class DurableOutboxWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<DurableOutboxWorkerOptions> options,
    ILogger<DurableOutboxWorker> logger)
    : BackgroundService
{
    private readonly DurableOutboxWorkerOptions _options = GetValidatedOptions(options);

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        ValidateDispatcherRegistration();

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            DurableOutboxDispatchResult? result = null;

            try
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                IDurableOutboxDispatcher dispatcher =
                    scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatcher>();

                result = await dispatcher.DispatchAsync(
                    _options.WorkerId,
                    _options.LeaseDuration,
                    _options.BatchSize,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Durable outbox worker {WorkerId} failed while dispatching a batch.",
                    _options.WorkerId);

                await DelayAsync(_options.FailureDelay, stoppingToken);
                continue;
            }

            if (result.ClaimedCount == 0)
            {
                await DelayAsync(_options.PollingInterval, stoppingToken);
            }
        }
    }

    private static async Task DelayAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static DurableOutboxWorkerOptions GetValidatedOptions(
        IOptions<DurableOutboxWorkerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        DurableOutboxWorkerOptions workerOptions = options.Value;
        workerOptions.Validate();
        return workerOptions;
    }

    private void ValidateDispatcherRegistration()
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatcher>();
    }
}
