using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableInboxHandlerExecutorTests
{
    private static readonly DateTimeOffset ReceivedAtUtc =
        new(2026, 6, 5, 8, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ProcessedAtUtc =
        new(2026, 6, 5, 8, 5, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenRecordIsNew_RunsHandlerAndMarksProcessed()
    {
        DurableInboxRecord record = CreateRecord();
        var registrar = new CapturingInboxRegistrar(
            new DurableInboxRegistrationResult(DurableInboxRegistrationStatus.Registered, record));
        var inboxStore = new CapturingInboxStore();
        var executor = new DurableInboxHandlerExecutor(
            registrar,
            inboxStore,
            new FixedTimeProvider(ProcessedAtUtc));
        var handlerCalls = 0;

        DurableInboxHandleResult result = await executor.HandleOnceAsync(
            record,
            _ =>
            {
                handlerCalls++;
                return ValueTask.CompletedTask;
            });

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.True(result.WasHandled);
        Assert.Equal(record.Key, result.Record.Key);
        Assert.Equal(ProcessedAtUtc, result.Record.ProcessedAtUtc);
        Assert.Same(record, registrar.Record);
        Assert.Equal(1, handlerCalls);
        Assert.Equal(record.Key, inboxStore.MarkedKey);
        Assert.Equal(ProcessedAtUtc, inboxStore.MarkedProcessedAtUtc);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(DurableInboxRegistrationStatus.AlreadyReceived, DurableInboxHandleStatus.AlreadyReceived)]
    [InlineData(DurableInboxRegistrationStatus.AlreadyProcessed, DurableInboxHandleStatus.AlreadyProcessed)]
    public async Task HandleOnceAsync_WhenRecordIsDuplicate_SkipsHandler(
        DurableInboxRegistrationStatus registrationStatus,
        DurableInboxHandleStatus expectedStatus)
    {
        DurableInboxRecord record = registrationStatus == DurableInboxRegistrationStatus.AlreadyProcessed
            ? CreateRecord().MarkProcessed(ProcessedAtUtc)
            : CreateRecord();
        var registrar = new CapturingInboxRegistrar(
            new DurableInboxRegistrationResult(registrationStatus, record));
        var inboxStore = new CapturingInboxStore();
        var executor = new DurableInboxHandlerExecutor(
            registrar,
            inboxStore,
            new FixedTimeProvider(ProcessedAtUtc));

        DurableInboxHandleResult result = await executor.HandleOnceAsync(
            record,
            _ => throw new InvalidOperationException("Handler should not run."));

        Assert.Equal(expectedStatus, result.Status);
        Assert.True(result.WasSkipped);
        Assert.Same(record, result.Record);
        Assert.Null(inboxStore.MarkedKey);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenHandlerThrows_DoesNotMarkProcessed()
    {
        DurableInboxRecord record = CreateRecord();
        var registrar = new CapturingInboxRegistrar(
            new DurableInboxRegistrationResult(DurableInboxRegistrationStatus.Registered, record));
        var inboxStore = new CapturingInboxStore();
        var executor = new DurableInboxHandlerExecutor(
            registrar,
            inboxStore,
            new FixedTimeProvider(ProcessedAtUtc));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.HandleOnceAsync(
                record,
                _ => throw new InvalidOperationException("Handler failed.")));

        Assert.Null(inboxStore.MarkedKey);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenMarkProcessedThrows_PropagatesException()
    {
        DurableInboxRecord record = CreateRecord();
        var registrar = new CapturingInboxRegistrar(
            new DurableInboxRegistrationResult(DurableInboxRegistrationStatus.Registered, record));
        var inboxStore = new CapturingInboxStore
        {
            MarkProcessedException = new InvalidOperationException("Mark failed."),
        };
        var executor = new DurableInboxHandlerExecutor(
            registrar,
            inboxStore,
            new FixedTimeProvider(ProcessedAtUtc));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await executor.HandleOnceAsync(
                record,
                _ => ValueTask.CompletedTask));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenRegistrarIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DurableInboxHandlerExecutor(
                null!,
                new CapturingInboxStore()));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenInboxStoreIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DurableInboxHandlerExecutor(
                new CapturingInboxRegistrar(
                    new DurableInboxRegistrationResult(
                        DurableInboxRegistrationStatus.Registered,
                        CreateRecord())),
                null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task HandleOnceAsync_WhenArgumentsAreNull_Throws()
    {
        var executor = new DurableInboxHandlerExecutor(
            new CapturingInboxRegistrar(
                new DurableInboxRegistrationResult(
                    DurableInboxRegistrationStatus.Registered,
                    CreateRecord())),
            new CapturingInboxStore());

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await executor.HandleOnceAsync(
                null!,
                _ => ValueTask.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await executor.HandleOnceAsync(
                CreateRecord(),
                null!));
    }

    private static DurableInboxRecord CreateRecord()
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(
                Guid.Parse("77b326f5-4186-46d2-bb46-40eefc0d8d45"),
                "sales",
                "sales.customer.registered.v1"),
            ReceivedAtUtc);
    }

    private sealed class CapturingInboxRegistrar(
        DurableInboxRegistrationResult result)
        : IDurableInboxRegistrar
    {
        public DurableInboxRecord? Record { get; private set; }

        public ValueTask<DurableInboxRegistrationResult> RegisterAsync(
            DurableInboxRecord record,
            CancellationToken ct = default)
        {
            Record = record;
            return ValueTask.FromResult(result);
        }
    }

    private sealed class CapturingInboxStore : IDurableInboxStore
    {
        public DurableInboxMessageKey? MarkedKey { get; private set; }

        public DateTimeOffset? MarkedProcessedAtUtc { get; private set; }

        public Exception? MarkProcessedException { get; init; }

        public ValueTask<DurableInboxRecord?> GetAsync(
            DurableInboxMessageKey key,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult<DurableInboxRecord?>(null);
        }

        public ValueTask AddAsync(
            DurableInboxRecord record,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }

        public ValueTask MarkProcessedAsync(
            DurableInboxMessageKey key,
            DateTimeOffset processedAtUtc,
            CancellationToken ct = default)
        {
            if (MarkProcessedException is not null)
            {
                throw MarkProcessedException;
            }

            MarkedKey = key;
            MarkedProcessedAtUtc = processedAtUtc;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
