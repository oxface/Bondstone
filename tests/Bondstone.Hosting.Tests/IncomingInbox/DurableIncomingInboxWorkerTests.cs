using Bondstone.Hosting.IncomingInbox;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bondstone.Hosting.Tests.IncomingInbox;

public sealed class DurableIncomingInboxWorkerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task StartAsync_WhenDispatcherReturnsNoClaims_PassesConfiguredWorkerOptions()
    {
        var dispatcher = new RecordingDispatcher(
            [new DurableIncomingInboxProcessingResult(0, 0, 0, 0, 0)]);
        DurableIncomingInboxWorker worker = CreateWorker(
            dispatcher,
            new DurableIncomingInboxWorkerOptions
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

        DurableIncomingInboxProcessingCall call = Assert.Single(dispatcher.Calls);
        Assert.Equal("worker-1", call.ClaimedBy);
        Assert.Equal(TimeSpan.FromMinutes(3), call.LeaseDuration);
        Assert.Equal(25, call.MaxCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task StartAsync_WhenRowsAreClaimed_ImmediatelyProcessesNextBatch()
    {
        var dispatcher = new RecordingDispatcher(
            [
                new DurableIncomingInboxProcessingResult(1, 1, 0, 0, 0),
                new DurableIncomingInboxProcessingResult(0, 0, 0, 0, 0),
            ]);
        DurableIncomingInboxWorker worker = CreateWorker(
            dispatcher,
            new DurableIncomingInboxWorkerOptions
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
    public async Task StartAsync_WhenDispatcherFails_LogsAndContinues()
    {
        var dispatcher = new RecordingDispatcher(
            [
                new InvalidOperationException("database unavailable"),
                new InvalidOperationException("database still unavailable"),
                new DurableIncomingInboxProcessingResult(0, 0, 0, 0, 0),
            ]);
        var logSink = new RecordingLogSink();
        DurableIncomingInboxWorker worker = CreateWorker(
            dispatcher,
            new DurableIncomingInboxWorkerOptions
            {
                WorkerId = "worker-1",
                FailureDelay = TimeSpan.FromMilliseconds(1),
                PollingInterval = TimeSpan.FromMinutes(10),
            },
            new RecordingLogger<DurableIncomingInboxWorker>(logSink));

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(timeout.Token);
        await dispatcher.WaitForCallCountAsync(3, timeout.Token);
        await worker.StopAsync(timeout.Token);

        Assert.Equal(3, dispatcher.Calls.Count);
        Assert.Contains(
            logSink.Entries,
            entry => entry.Level == LogLevel.Error
                && entry.EventId.Id == 2001
                && entry.EventId.Name == "ProcessBatchFailed"
                && entry.Message.Contains("worker-1", StringComparison.Ordinal)
                && entry.Message.Contains("Consecutive failure count: 2", StringComparison.Ordinal)
                && entry.Exception is InvalidOperationException);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task StopAsync_WhenDispatcherObservesCancellation_StopsCleanly()
    {
        var dispatcher = new BlockingDispatcher();
        DurableIncomingInboxWorker worker = CreateWorker(
            dispatcher,
            new DurableIncomingInboxWorkerOptions { WorkerId = "worker-1" });

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await worker.StartAsync(timeout.Token);
        await dispatcher.WaitForCallAsync(timeout.Token);

        await worker.StopAsync(timeout.Token);

        Assert.Equal(1, dispatcher.CallCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenOptionsAreInvalid_Throws()
    {
        var dispatcher = new RecordingDispatcher([]);

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateWorker(
                dispatcher,
                new DurableIncomingInboxWorkerOptions { WorkerId = " " }));

        Assert.Equal(nameof(DurableIncomingInboxWorkerOptions.WorkerId), exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task StartAsync_WhenDispatcherIsMissing_Throws()
    {
        await using ServiceProvider serviceProvider = new ServiceCollection().BuildServiceProvider();
        var worker = new DurableIncomingInboxWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new DurableIncomingInboxWorkerOptions()),
            NullLogger<DurableIncomingInboxWorker>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => worker.StartAsync(CancellationToken.None));
    }

    private static DurableIncomingInboxWorker CreateWorker(
        IDurableIncomingInboxDispatcher dispatcher,
        DurableIncomingInboxWorkerOptions options,
        ILogger<DurableIncomingInboxWorker>? logger = null)
    {
        ServiceProvider serviceProvider = new ServiceCollection()
            .AddScoped(_ => dispatcher)
            .BuildServiceProvider();

        return new DurableIncomingInboxWorker(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            logger ?? NullLogger<DurableIncomingInboxWorker>.Instance);
    }

    private sealed class RecordingDispatcher(IReadOnlyList<object> outcomes)
        : IDurableIncomingInboxDispatcher
    {
        private readonly object _syncRoot = new();
        private int _nextOutcomeIndex;

        public List<DurableIncomingInboxProcessingCall> Calls { get; } = [];

        public ValueTask<DurableIncomingInboxProcessingResult> ProcessAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken ct = default)
        {
            lock (_syncRoot)
            {
                Calls.Add(new DurableIncomingInboxProcessingCall(claimedBy, leaseDuration, maxCount));
            }

            object outcome = _nextOutcomeIndex < outcomes.Count
                ? outcomes[_nextOutcomeIndex++]
                : new DurableIncomingInboxProcessingResult(0, 0, 0, 0, 0);

            if (outcome is Exception exception)
            {
                throw exception;
            }

            return ValueTask.FromResult((DurableIncomingInboxProcessingResult)outcome);
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

    private sealed class BlockingDispatcher : IDurableIncomingInboxDispatcher
    {
        private readonly TaskCompletionSource _called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public int CallCount => _callCount;

        public async ValueTask<DurableIncomingInboxProcessingResult> ProcessAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken ct = default)
        {
            Interlocked.Increment(ref _callCount);
            _called.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);

            return new DurableIncomingInboxProcessingResult(0, 0, 0, 0, 0);
        }

        public async Task WaitForCallAsync(CancellationToken ct)
        {
            await _called.Task.WaitAsync(ct);
        }
    }

    private sealed record DurableIncomingInboxProcessingCall(
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
