using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableOperationResultReaderTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResultAsync_WhenOperationStateDoesNotExist_ReturnsUnknownResult()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(durableOperationId);

        Assert.Equal(durableOperationId, result.DurableOperationId);
        Assert.False(result.IsKnown);
        Assert.False(result.HasResult);
        Assert.Null(result.Result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResultAsync_WhenOperationCompletedWithResultPayload_DeserializesResult()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"),
                """{"orderId":"payload-A-100","accepted":true}"""),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(durableOperationId);

        Assert.True(result.IsKnown);
        Assert.True(result.IsCompleted);
        Assert.True(result.IsTerminal);
        Assert.True(result.HasResult);
        Assert.Equal(DurableOperationStatus.Completed, result.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"), result.UpdatedAtUtc);
        Assert.Equal(new ReserveOrderResult(new DurableOrderId("A-100"), Accepted: true), result.Result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResultAsync_WhenOperationFailed_ReturnsTerminalFailureWithoutResult()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Failed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"),
                failureReason: "handler failed"),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(durableOperationId);

        Assert.True(result.IsKnown);
        Assert.False(result.IsCompleted);
        Assert.True(result.IsTerminal);
        Assert.False(result.HasResult);
        Assert.Equal(DurableOperationStatus.Failed, result.Status);
        Assert.Equal("handler failed", result.FailureReason);
        Assert.Null(result.Result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task WaitForResultAsync_WhenOperationEventuallyCompletes_ReturnsCompletedResult()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new SequencedModuleOperationStateStore(
            "fulfillment",
            new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-08T12:00:00+00:00")),
            new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"),
                """{"orderId":"payload-A-100","accepted":true}"""));
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .WaitForResultAsync<ReserveOrderResult>(
                durableOperationId,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(1));

        Assert.True(result.IsCompleted);
        Assert.True(result.HasResult);
        Assert.Equal(new ReserveOrderResult(new DurableOrderId("A-100"), Accepted: true), result.Result);
        Assert.True(store.ReadCount >= 2);
    }

    private static ServiceProvider CreateServiceProvider(
        params IModuleOperationStateStore[] stores)
    {
        var services = new ServiceCollection();
        services.ConfigureBondstoneDurablePayloadJson(
            options => options.Converters.Add(new DurableOrderIdJsonConverter()));

        services.AddBondstone(bondstone =>
        {
            foreach (IModuleOperationStateStore store in stores)
            {
                bondstone.Services.GetOrAddDurableModulePersistenceRegistrationRegistry()
                    .AddOperationStateStore(new DurableModuleOperationStateStoreRegistration(
                        store.ModuleName,
                        _ => store));
                bondstone.Module(store.ModuleName, _ => { });
            }
        });

        return services.BuildServiceProvider();
    }

    public sealed record ReserveOrderResult(DurableOrderId OrderId, bool Accepted);

    private interface IModuleOperationStateStore : IDurableOperationStateStore
    {
        string ModuleName { get; }
    }

    private sealed class CapturingModuleOperationStateStore(string moduleName)
        : IModuleOperationStateStore
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

    private sealed class SequencedModuleOperationStateStore(
        string moduleName,
        params DurableOperationState[] states)
        : IModuleOperationStateStore
    {
        private readonly Queue<DurableOperationState> _states = new(states);

        public string ModuleName { get; } = moduleName;

        public int ReadCount { get; private set; }

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            ReadCount++;
            if (_states.Count > 1)
            {
                return ValueTask.FromResult<DurableOperationState?>(_states.Dequeue());
            }

            return ValueTask.FromResult<DurableOperationState?>(_states.Peek());
        }

        public ValueTask SaveAsync(
            DurableOperationState state,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }
}
