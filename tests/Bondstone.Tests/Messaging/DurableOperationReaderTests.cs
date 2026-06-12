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
        var fallbackReader = new CapturingOperationReader
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            moduleStore,
            fallbackReader);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Pending, state.Status);
        Assert.Equal(0, fallbackReader.ReadCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenOnlyRootReaderWasRegisteredBeforeBondstone_UsesFallbackReader()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var fallbackReader = new CapturingOperationReader
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(fallbackReader);

        DurableOperationState? state = await serviceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Completed, state.Status);
        Assert.Equal(1, fallbackReader.ReadCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenScopedRootReaderWasRegisteredBeforeBondstone_DisposesFallbackWithScope()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var capture = new DisposableReaderCapture();
        var services = new ServiceCollection();
        services.AddScoped<IDurableOperationReader>(_ => capture.Create());
        services.AddBondstone(_ => { });
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        DisposableOperationReader fallbackReader;
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            DurableOperationState? state = await scope.ServiceProvider
                .GetRequiredService<IDurableOperationReader>()
                .GetStateAsync(durableOperationId);

            Assert.NotNull(state);
            Assert.Equal(DurableOperationStatus.Completed, state.Status);
            fallbackReader = Assert.Single(capture.Readers);
            Assert.Equal(1, fallbackReader.ReadCount);
            Assert.False(fallbackReader.Disposed);
        }

        Assert.True(fallbackReader.Disposed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenTransientRootReaderWasRegisteredBeforeBondstone_CreatesFallbackPerReadAndDisposesWithScope()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var capture = new DisposableReaderCapture();
        var services = new ServiceCollection();
        services.AddTransient<IDurableOperationReader>(_ => capture.Create());
        services.AddBondstone(_ => { });
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            IDurableOperationReader reader = scope.ServiceProvider
                .GetRequiredService<IDurableOperationReader>();

            await reader.GetStateAsync(durableOperationId);
            await reader.GetStateAsync(durableOperationId);

            Assert.Equal(2, capture.Readers.Count);
            Assert.All(capture.Readers, fallbackReader =>
            {
                Assert.Equal(1, fallbackReader.ReadCount);
                Assert.False(fallbackReader.Disposed);
            });
        }

        Assert.All(capture.Readers, fallbackReader => Assert.True(fallbackReader.Disposed));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenSingletonInstanceRootReaderWasRegisteredBeforeBondstone_DoesNotDisposeExternalInstance()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var fallbackReader = new DisposableOperationReader();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOperationReader>(fallbackReader);
        services.AddBondstone(_ => { });
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            DurableOperationState? state = await scope.ServiceProvider
                .GetRequiredService<IDurableOperationReader>()
                .GetStateAsync(durableOperationId);

            Assert.NotNull(state);
            Assert.Equal(DurableOperationStatus.Completed, state.Status);
        }

        await serviceProvider.DisposeAsync();

        Assert.Equal(1, fallbackReader.ReadCount);
        Assert.False(fallbackReader.Disposed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenAsyncDisposableRootReaderWasRegisteredBeforeBondstone_DisposesFallbackWithAsyncScope()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var capture = new AsyncDisposableReaderCapture();
        var services = new ServiceCollection();
        services.AddScoped<IDurableOperationReader>(_ => capture.Create());
        services.AddBondstone(_ => { });
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        AsyncDisposableOperationReader fallbackReader;
        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            DurableOperationState? state = await scope.ServiceProvider
                .GetRequiredService<IDurableOperationReader>()
                .GetStateAsync(durableOperationId);

            Assert.NotNull(state);
            Assert.Equal(DurableOperationStatus.Completed, state.Status);
            fallbackReader = Assert.Single(capture.Readers);
            Assert.Equal(1, fallbackReader.ReadCount);
            Assert.False(fallbackReader.DisposedAsync);
        }

        Assert.True(fallbackReader.DisposedAsync);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetStateAsync_WhenAsyncDisposableRootReaderWasRegisteredBeforeBondstone_RequiresAsyncScopeDisposal()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var capture = new AsyncDisposableReaderCapture();
        var services = new ServiceCollection();
        services.AddScoped<IDurableOperationReader>(_ => capture.Create());
        services.AddBondstone(_ => { });
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        AsyncDisposableOperationReader fallbackReader = Assert.Single(capture.Readers);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            scope.Dispose);

        Assert.Contains(nameof(IAsyncDisposable), exception.Message, StringComparison.Ordinal);
        Assert.Contains("asynchronously", exception.Message, StringComparison.Ordinal);
        Assert.False(fallbackReader.DisposedAsync);
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

    private sealed class DisposableReaderCapture
    {
        public List<DisposableOperationReader> Readers { get; } = [];

        public DisposableOperationReader Create()
        {
            var reader = new DisposableOperationReader();
            Readers.Add(reader);
            return reader;
        }
    }

    private sealed class AsyncDisposableReaderCapture
    {
        public List<AsyncDisposableOperationReader> Readers { get; } = [];

        public AsyncDisposableOperationReader Create()
        {
            var reader = new AsyncDisposableOperationReader();
            Readers.Add(reader);
            return reader;
        }
    }

    private sealed class AsyncDisposableOperationReader : IDurableOperationReader, IAsyncDisposable
    {
        public int ReadCount { get; private set; }

        public bool DisposedAsync { get; private set; }

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            ReadCount++;
            return ValueTask.FromResult<DurableOperationState?>(
                new DurableOperationState(
                    durableOperationId,
                    DurableOperationStatus.Completed,
                    DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")));
        }

        public ValueTask DisposeAsync()
        {
            DisposedAsync = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class DisposableOperationReader : IDurableOperationReader, IDisposable
    {
        public int ReadCount { get; private set; }

        public bool Disposed { get; private set; }

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            ReadCount++;
            return ValueTask.FromResult<DurableOperationState?>(
                new DurableOperationState(
                    durableOperationId,
                    DurableOperationStatus.Completed,
                    DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")));
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
