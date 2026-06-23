using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
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
    public async Task GetStateAsync_WhenModuleStoresAreRegistered_DoesNotUseRootOperationStateStore()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var moduleStore = new CapturingModuleOperationStateStore("sales")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-08T12:00:00+00:00")),
        };
        var rootStore = new CapturingOperationStateStore
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            moduleStore,
            rootStore);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Pending, state.Status);
        Assert.Equal(0, rootStore.ReadCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenModuleHintIsProvided_ReadsOnlyHintedModuleStore()
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
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            salesStore,
            fulfillmentStore);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(
                durableOperationId,
                " sales ");

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Pending, state.Status);
        Assert.Equal(1, salesStore.ReadCount);
        Assert.Equal(0, fulfillmentStore.ReadCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenOperationHandleIsProvided_ReadsOnlyTargetModuleStore()
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
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            salesStore,
            fulfillmentStore);
        var operation = new DurableOperationHandle(
            durableOperationId,
            "sales",
            "fulfillment");

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(operation);

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Completed, state.Status);
        Assert.Equal(0, salesStore.ReadCount);
        Assert.Equal(1, fulfillmentStore.ReadCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenModuleHintStoreIsMissing_ThrowsClearError()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        await using ServiceProvider serviceProvider = CreateServiceProvider();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await serviceProvider
                .GetRequiredService<IDurableOperationReader>()
                .GetStateAsync(
                    durableOperationId,
                    "fulfillment"));

        Assert.Contains("durable module operation-state store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenRootReaderWasRegisteredBeforeBondstoneAndModuleStoresExist_UsesModuleStores()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var moduleStore = new CapturingModuleOperationStateStore("sales")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-08T12:00:00+00:00")),
        };
        var rootReader = new CapturingOperationReader
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            moduleStore,
            rootReader);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Pending, state.Status);
        Assert.Equal(0, rootReader.ReadCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenOnlyRootReaderWasRegisteredBeforeBondstone_ReturnsNull()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var rootReader = new CapturingOperationReader
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(rootReader);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.Null(state);
        Assert.Equal(0, rootReader.ReadCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenOnlyRootOperationStateStoreIsRegistered_ReturnsNull()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var rootStore = new CapturingOperationStateStore
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(rootStore);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.Null(state);
        Assert.Equal(0, rootStore.ReadCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenRootReaderIsRegisteredInsideBondstoneConfiguration_ReturnsNull()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var rootReader = new CapturingOperationReader
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        var services = new ServiceCollection();
        services.AddBondstone(bondstone =>
        {
            bondstone.Services.AddSingleton<IDurableOperationReader>(rootReader);
        });
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.Null(state);
        Assert.Equal(0, rootReader.ReadCount);
    }

    private static ServiceProvider CreateServiceProvider(
        params object[] storesOrReaders)
    {
        var services = new ServiceCollection();
        var moduleNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (object storeOrReader in storesOrReaders)
        {
            switch (storeOrReader)
            {
                case CapturingModuleOperationStateStore moduleStore:
                    moduleNames.Add(moduleStore.ModuleName);
                    services.GetOrAddDurableModulePersistenceRegistrationRegistry()
                        .AddOperationStateStore(new DurableModuleOperationStateStoreRegistration(
                            moduleStore.ModuleName,
                            _ => moduleStore));
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

        services.AddBondstone(bondstone =>
        {
            foreach (string moduleName in moduleNames)
            {
                bondstone.Module(moduleName, _ => { });
            }
        });
        return services.BuildServiceProvider();
    }

    private sealed class CapturingModuleOperationStateStore(string moduleName)
        : IDurableOperationStateStore
    {
        public string ModuleName { get; } = moduleName;

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

    private sealed class CapturingOperationReader : IDurableOperationReader
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
    }

}
