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

    public override Task StartAsync(CancellationToken ct)
    {
        ValidateDispatcherRegistration();

        return base.StartAsync(ct);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
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
                    ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Durable outbox worker {WorkerId} failed while dispatching a batch.",
                    _options.WorkerId);

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
