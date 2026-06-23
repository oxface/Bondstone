using System.Diagnostics.Metrics;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableOperationExpirationProcessorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkExpiredAsync_WhenCandidatesExist_FinalizesPendingAndRunningOperations()
    {
        Guid pendingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid runningId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid freshId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        Guid completedId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var store = new ExpiringModuleOperationStateStore("fulfillment");
        store.Seed(
            new DurableOperationState(
                pendingId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-16T10:00:00+00:00")),
            new DurableOperationState(
                runningId,
                DurableOperationStatus.Running,
                DateTimeOffset.Parse("2026-06-16T10:01:00+00:00")),
            new DurableOperationState(
                freshId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-16T10:05:00+00:00")),
            new DurableOperationState(
                completedId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-16T10:00:00+00:00")));
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationExpirationResult result = await serviceProvider
            .GetRequiredService<IDurableOperationExpirationProcessor>()
            .MarkExpiredAsync(
                "fulfillment",
                DateTimeOffset.Parse("2026-06-16T10:01:00+00:00"),
                DurableOperationStatus.Failed,
                "operation expired",
                maxCount: 10);

        Assert.Equal("fulfillment", result.ModuleName);
        Assert.Equal(DurableOperationStatus.Failed, result.TerminalStatus);
        Assert.Equal(2, result.CandidateCount);
        Assert.Equal(2, result.FinalizedCount);
        Assert.Collection(
            result.Finalizations,
            first =>
            {
                Assert.Equal(pendingId, first.DurableOperationId);
                Assert.Equal(DurableOperationStatus.Failed, first.Status);
                Assert.True(first.WasFinalized);
            },
            second =>
            {
                Assert.Equal(runningId, second.DurableOperationId);
                Assert.Equal(DurableOperationStatus.Failed, second.Status);
                Assert.True(second.WasFinalized);
            });

        Assert.Equal(DurableOperationStatus.Failed, store.GetSaved(pendingId).Status);
        Assert.Equal(DurableOperationStatus.Failed, store.GetSaved(runningId).Status);
        Assert.Equal(DurableOperationStatus.Pending, store.GetSaved(freshId).Status);
        Assert.Equal(DurableOperationStatus.Completed, store.GetSaved(completedId).Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkExpiredAsync_WhenCandidatesExist_EmitsExpirationMetrics()
    {
        Guid pendingId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid runningId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var store = new ExpiringModuleOperationStateStore("metric-fulfillment");
        store.Seed(
            new DurableOperationState(
                pendingId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-16T10:00:00+00:00")),
            new DurableOperationState(
                runningId,
                DurableOperationStatus.Running,
                DateTimeOffset.Parse("2026-06-16T10:01:00+00:00")));
        var measurements = new List<MetricMeasurement>();
        using MeterListener listener = MetricTestHelper.CreateMeterListener(
            "Bondstone.Modules",
            measurements);
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        await serviceProvider
            .GetRequiredService<IDurableOperationExpirationProcessor>()
            .MarkExpiredAsync(
                "metric-fulfillment",
                DateTimeOffset.Parse("2026-06-16T10:01:00+00:00"),
                DurableOperationStatus.Cancelled,
                "operation expired",
                maxCount: 10);

        MetricMeasurement candidates = Assert.Single(
            measurements,
            candidate =>
                candidate.InstrumentName == "bondstone.operation.expiration.candidates"
                && candidate.HasTag("bondstone.module", "metric-fulfillment"));
        Assert.Equal(2, candidates.Value);
        Assert.Equal("Cancelled", candidates.GetTag("bondstone.operation_status"));

        MetricMeasurement finalized = Assert.Single(
            measurements,
            candidate =>
                candidate.InstrumentName == "bondstone.operation.expiration.finalized"
                && candidate.HasTag("bondstone.module", "metric-fulfillment"));
        Assert.Equal(2, finalized.Value);
        Assert.Equal("Cancelled", finalized.GetTag("bondstone.operation_status"));

        Assert.Equal(
            2,
            MetricTestHelper.FindMeasurements(
                    measurements,
                    "bondstone.operation.finalized",
                    "bondstone.module",
                    "metric-fulfillment")
                .Sum(static measurement => measurement.Value));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkExpiredAsync_WhenMaxCountLimitsCandidates_FinalizesOnlyReturnedCandidates()
    {
        Guid firstId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid secondId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var store = new ExpiringModuleOperationStateStore("fulfillment");
        store.Seed(
            new DurableOperationState(
                firstId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-16T10:00:00+00:00")),
            new DurableOperationState(
                secondId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-16T10:01:00+00:00")));
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationExpirationResult result = await serviceProvider
            .GetRequiredService<IDurableOperationExpirationProcessor>()
            .MarkExpiredAsync(
                "fulfillment",
                DateTimeOffset.Parse("2026-06-16T10:05:00+00:00"),
                DurableOperationStatus.Cancelled,
                "cancelled by expiry policy",
                maxCount: 1);

        DurableOperationFinalizationResult finalization = Assert.Single(result.Finalizations);
        Assert.Equal(firstId, finalization.DurableOperationId);
        Assert.Equal(DurableOperationStatus.Cancelled, store.GetSaved(firstId).Status);
        Assert.Equal(DurableOperationStatus.Pending, store.GetSaved(secondId).Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkExpiredAsync_WhenStoreDoesNotSupportExpiration_ThrowsClearError()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            new NonExpiringModuleOperationStateStore("fulfillment"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await serviceProvider
                .GetRequiredService<IDurableOperationExpirationProcessor>()
                .MarkExpiredAsync(
                    "fulfillment",
                    DateTimeOffset.Parse("2026-06-16T10:05:00+00:00"),
                    DurableOperationStatus.Failed,
                    "expired"));

        Assert.Contains(nameof(IDurableOperationExpirationStore), exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkExpiredAsync_WhenTerminalStatusIsCompleted_Throws()
    {
        var store = new ExpiringModuleOperationStateStore("fulfillment");
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await serviceProvider
                .GetRequiredService<IDurableOperationExpirationProcessor>()
                .MarkExpiredAsync(
                    "fulfillment",
                    DateTimeOffset.Parse("2026-06-16T10:05:00+00:00"),
                    DurableOperationStatus.Completed,
                    "expired"));
    }

    private static ServiceProvider CreateServiceProvider(
        IModuleOperationStateStore store)
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Services.GetOrAddDurableModulePersistenceRegistrationRegistry()
                .AddOperationStateStore(new DurableModuleOperationStateStoreRegistration(
                    store.ModuleName,
                    _ => store));
            bondstone.Module(store.ModuleName, _ => { });
        });

        return services.BuildServiceProvider();
    }

    private interface IModuleOperationStateStore : IDurableOperationStateStore
    {
        string ModuleName { get; }
    }

    private sealed class ExpiringModuleOperationStateStore(string moduleName)
        : IModuleOperationStateStore,
            IDurableOperationExpirationStore
    {
        private readonly Dictionary<Guid, DurableOperationState> _states = [];

        public string ModuleName { get; } = moduleName;

        public void Seed(params DurableOperationState[] states)
        {
            foreach (DurableOperationState state in states)
            {
                _states[state.DurableOperationId] = state;
            }
        }

        public DurableOperationState GetSaved(Guid durableOperationId) =>
            _states[durableOperationId];

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            _states.TryGetValue(
                durableOperationId,
                out DurableOperationState? state);
            return ValueTask.FromResult(state);
        }

        public ValueTask SaveAsync(
            DurableOperationState state,
            CancellationToken ct = default)
        {
            _states[state.DurableOperationId] = state;
            return ValueTask.CompletedTask;
        }

        public ValueTask<IReadOnlyList<DurableOperationState>> FindExpirationCandidatesAsync(
            DateTimeOffset expiresBeforeUtc,
            int maxCount,
            CancellationToken ct = default)
        {
            IReadOnlyList<DurableOperationState> states = _states.Values
                .Where(static state =>
                    state.Status is DurableOperationStatus.Pending or DurableOperationStatus.Running)
                .Where(state => state.UpdatedAtUtc <= expiresBeforeUtc)
                .OrderBy(static state => state.UpdatedAtUtc)
                .Take(maxCount)
                .ToArray();

            return ValueTask.FromResult(states);
        }
    }

    private sealed class NonExpiringModuleOperationStateStore(string moduleName)
        : IModuleOperationStateStore
    {
        public string ModuleName { get; } = moduleName;

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult<DurableOperationState?>(null);
        }

        public ValueTask SaveAsync(
            DurableOperationState state,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }
}
