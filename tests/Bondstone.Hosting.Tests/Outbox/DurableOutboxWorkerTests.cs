using Bondstone.Hosting.Outbox;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bondstone.Hosting.Tests.Outbox;

public sealed class DurableOutboxWorkerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task StartAsync_WhenDispatcherReturnsNoClaims_PassesConfiguredWorkerOptions()
    {
        var dispatcher = new RecordingDispatcher(
            [new DurableOutboxDispatchResult(0, 0, 0, 0, 0)]);
        DurableOutboxWorker worker = CreateWorker(
            dispatcher,
            new DurableOutboxWorkerOptions
            {
                WorkerId = " worker-1 ",
                LeaseDuration = TimeSpan.FromMinutes(3),
                BatchSize = 25,
                PollingInterval = TimeSpan.FromMinutes(10),
            });

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(timeout.Token);
        await dispatcher.WaitForCallCountAsync(1, timeout.Token);
        await worker.StopAsync(timeout.Token);

        DurableOutboxDispatchCall call = Assert.Single(dispatcher.Calls);
        Assert.Equal("worker-1", call.ClaimedBy);
        Assert.Equal(TimeSpan.FromMinutes(3), call.LeaseDuration);
        Assert.Equal(25, call.MaxCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task StartAsync_WhenRowsAreClaimed_ImmediatelyDispatchesNextBatch()
    {
        var dispatcher = new RecordingDispatcher(
            [
                new DurableOutboxDispatchResult(1, 1, 0, 0, 0),
                new DurableOutboxDispatchResult(0, 0, 0, 0, 0),
            ]);
        DurableOutboxWorker worker = CreateWorker(
            dispatcher,
            new DurableOutboxWorkerOptions
            {
                WorkerId = "worker-1",
                PollingInterval = TimeSpan.FromMinutes(10),
            });

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(timeout.Token);
        await dispatcher.WaitForCallCountAsync(2, timeout.Token);
        await worker.StopAsync(timeout.Token);

        Assert.Equal(2, dispatcher.Calls.Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task StartAsync_WhenDispatcherFails_WaitsFailureDelayAndContinues()
    {
        var dispatcher = new RecordingDispatcher(
            [
                new InvalidOperationException("database unavailable"),
                new DurableOutboxDispatchResult(0, 0, 0, 0, 0),
            ]);
        var logSink = new RecordingLogSink();
        DurableOutboxWorker worker = CreateWorker(
            dispatcher,
            new DurableOutboxWorkerOptions
            {
                WorkerId = "worker-1",
                FailureDelay = TimeSpan.FromMilliseconds(1),
                PollingInterval = TimeSpan.FromMinutes(10),
            },
            new RecordingLogger<DurableOutboxWorker>(logSink));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(timeout.Token);
        await dispatcher.WaitForCallCountAsync(2, timeout.Token);
        await worker.StopAsync(timeout.Token);

        Assert.Equal(2, dispatcher.Calls.Count);
        Assert.Contains(
            logSink.Entries,
            entry => entry.Level == LogLevel.Error
                && entry.EventId.Id == 1001
                && entry.EventId.Name == "DispatchBatchFailed"
                && entry.Message.Contains("worker-1", StringComparison.Ordinal)
                && entry.Exception is InvalidOperationException);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenOptionsAreInvalid_Throws()
    {
        var dispatcher = new RecordingDispatcher([]);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateWorker(
                dispatcher,
                new DurableOutboxWorkerOptions { WorkerId = " " }));

        Assert.Equal(nameof(DurableOutboxWorkerOptions.WorkerId), exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task StartAsync_WhenDispatcherIsMissing_Throws()
    {
        await using ServiceProvider serviceProvider = new ServiceCollection().BuildServiceProvider();
        var worker = new DurableOutboxWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DurableOutboxWorkerOptions()),
            NullLogger<DurableOutboxWorker>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => worker.StartAsync(CancellationToken.None));
    }

    private static DurableOutboxWorker CreateWorker(
        RecordingDispatcher dispatcher,
        DurableOutboxWorkerOptions options,
        ILogger<DurableOutboxWorker>? logger = null)
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddScoped<IDurableOutboxDispatcher>(_ => dispatcher)
            .BuildServiceProvider();

        return new DurableOutboxWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            logger ?? NullLogger<DurableOutboxWorker>.Instance);
    }

    private sealed class RecordingDispatcher(IReadOnlyList<object> outcomes)
        : IDurableOutboxDispatcher
    {
        private readonly object _syncRoot = new();
        private int _nextOutcomeIndex;

        public List<DurableOutboxDispatchCall> Calls { get; } = [];

        public ValueTask<DurableOutboxDispatchResult> DispatchAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken ct = default)
        {
            lock (_syncRoot)
            {
                Calls.Add(new DurableOutboxDispatchCall(claimedBy, leaseDuration, maxCount));
            }

            object outcome = _nextOutcomeIndex < outcomes.Count
                ? outcomes[_nextOutcomeIndex++]
                : new DurableOutboxDispatchResult(0, 0, 0, 0, 0);

            if (outcome is Exception exception)
            {
                throw exception;
            }

            return ValueTask.FromResult((DurableOutboxDispatchResult)outcome);
        }

        public async Task WaitForCallCountAsync(
            int expectedCallCount,
            CancellationToken ct)
        {
            while (true)
            {
                lock (_syncRoot)
                {
                    if (Calls.Count >= expectedCallCount)
                    {
                        return;
                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(1), ct);
            }
        }
    }

    private sealed record DurableOutboxDispatchCall(
        string ClaimedBy,
        TimeSpan LeaseDuration,
        int MaxCount);

    private sealed class RecordingLogSink
    {
        private readonly List<RecordingLogEntry> _entries = [];
        private readonly Lock _lock = new();

        public IReadOnlyCollection<RecordingLogEntry> Entries
        {
            get
            {
                lock (_lock)
                {
                    return _entries.ToArray();
                }
            }
        }

        public void Add(
            RecordingLogEntry entry)
        {
            lock (_lock)
            {
                _entries.Add(entry);
            }
        }
    }

    private sealed class RecordingLogger<T>(
        RecordingLogSink sink)
        : ILogger<T>
    {
        public IDisposable BeginScope<TState>(
            TState state)
            where TState : notnull
        {
            return NoopDisposable.Instance;
        }

        public bool IsEnabled(
            LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            sink.Add(new RecordingLogEntry(
                logLevel,
                eventId,
                formatter(state, exception),
                exception));
        }
    }

    private sealed record RecordingLogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception);

    private sealed class NoopDisposable : IDisposable
    {
        public static readonly NoopDisposable Instance = new();

        public void Dispose()
        {
        }
    }
}
