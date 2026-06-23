using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bondstone.Hosting.IncomingInbox;

internal sealed class DurableIncomingInboxWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<DurableIncomingInboxWorkerOptions> options,
    ILogger<DurableIncomingInboxWorker> logger)
    : BackgroundService
{
    private readonly DurableIncomingInboxWorkerOptions _options = GetValidatedOptions(options);

    public override Task StartAsync(CancellationToken ct)
    {
        ValidateDispatcherRegistration();

        return base.StartAsync(ct);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var consecutiveFailureCount = 0;

        while (!ct.IsCancellationRequested)
        {
            DurableIncomingInboxProcessingResult? result = null;

            try
            {
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                IDurableIncomingInboxDispatcher dispatcher =
                    scope.ServiceProvider.GetRequiredService<IDurableIncomingInboxDispatcher>();

                result = await dispatcher.ProcessAsync(
                    _options.WorkerId,
                    _options.LeaseDuration,
                    _options.BatchSize,
                    ct);
                consecutiveFailureCount = 0;
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                consecutiveFailureCount++;
                logger.LogError(
                    DurableIncomingInboxWorkerLogEvents.ProcessBatchFailed,
                    exception,
                    "Durable incoming inbox worker {WorkerId} failed while processing a batch. Consecutive failure count: {ConsecutiveFailureCount}.",
                    _options.WorkerId,
                    consecutiveFailureCount);

                await DelayAsync(_options.FailureDelay, ct);
                continue;
            }

            if (result.ClaimedCount == 0)
            {
                await DelayAsync(_options.PollingInterval, ct);
            }
        }
    }

    private static async Task DelayAsync(
        TimeSpan delay,
        CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private static DurableIncomingInboxWorkerOptions GetValidatedOptions(
        IOptions<DurableIncomingInboxWorkerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        DurableIncomingInboxWorkerOptions workerOptions = options.Value;
        workerOptions.Validate();
        return workerOptions;
    }

    private void ValidateDispatcherRegistration()
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IDurableIncomingInboxDispatcher>();
    }
}
