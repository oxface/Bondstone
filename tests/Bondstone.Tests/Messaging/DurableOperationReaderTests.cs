using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableOperationReaderTests
{
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(DurableOperationStatus.Completed)]
    [InlineData(DurableOperationStatus.Failed)]
    [InlineData(DurableOperationStatus.Cancelled)]
    public async Task GetStateAsync_WhenModuleStoresHavePendingAndTerminalState_ReturnsTerminalState(
        DurableOperationStatus terminalStatus)
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var salesStore = new CapturingModuleOperationStateStore("sales")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-08T12:00:00+00:00")),
        };
        var fulfillmentStore = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                terminalStatus,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            salesStore,
            fulfillmentStore);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.NotNull(state);
        Assert.Equal(terminalStatus, state.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"), state.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenModuleStoresHaveSameRank_ReturnsNewestState()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var olderStore = new CapturingModuleOperationStateStore("sales")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        var newerStore = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Failed,
                DateTimeOffset.Parse("2026-06-08T12:02:00+00:00"),
                failureReason: "terminal receive failure"),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            olderStore,
            newerStore);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Failed, state.Status);
        Assert.Equal("terminal receive failure", state.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenModuleStoresAreRegistered_DoesNotUseFallbackReader()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var moduleStore = new CapturingModuleOperationStateStore("sales")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-08T12:00:00+00:00")),
        };
        var fallbackStore = new CapturingOperationStateStore
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            moduleStore,
            fallbackStore);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Pending, state.Status);
        Assert.Equal(0, fallbackStore.ReadCount);
    }

    private static ServiceProvider CreateServiceProvider(
        params object[] storesOrReaders)
    {
        var services = new ServiceCollection();
        foreach (object storeOrReader in storesOrReaders)
        {
            switch (storeOrReader)
            {
                case IDurableModuleOperationStateStore moduleStore:
                    services.AddSingleton(moduleStore);
                    break;
                case IDurableOperationStateStore operationStateStore:
                    services.AddSingleton(operationStateStore);
                    break;
                case IDurableOperationReader operationReader:
                    services.AddSingleton(operationReader);
                    break;
                default:
                    throw new ArgumentException(
                        $"Unsupported service '{storeOrReader.GetType().FullName}'.",
                        nameof(storesOrReaders));
            }
        }

        services.AddBondstone(_ => { });
        return services.BuildServiceProvider();
    }

    private sealed class CapturingModuleOperationStateStore(string moduleName)
        : IDurableModuleOperationStateStore
    {
        public string ModuleName { get; } = moduleName;

        public DurableOperationState? State { get; init; }

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult(State);
        }

        public ValueTask SaveAsync(
            DurableOperationState state,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingOperationStateStore : IDurableOperationStateStore
    {
        public DurableOperationState? State { get; init; }

        public int ReadCount { get; private set; }

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            ReadCount++;
            return ValueTask.FromResult(State);
        }

        public ValueTask SaveAsync(
            DurableOperationState state,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }
}
