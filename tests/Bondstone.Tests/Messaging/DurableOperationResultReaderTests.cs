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
        Assert.Equal(DurableOperationResultState.Unknown, result.State);
        Assert.False(result.IsKnown);
        Assert.False(result.IsCompleted);
        Assert.False(result.IsTerminal);
        Assert.False(result.HasResult);
        Assert.Null(result.DeserializationFailure);
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
                """{"orderId":"payload-A-100","accepted":true}""",
                diagnosticContext: new DurableOperationDiagnosticContext(
                    "fulfillment",
                    "fulfillment.order.reserve.v1",
                    "receive.fulfillment.order.reserve.v1")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(durableOperationId);

        Assert.True(result.IsKnown);
        Assert.True(result.IsCompleted);
        Assert.True(result.IsTerminal);
        Assert.True(result.HasResult);
        Assert.Equal(DurableOperationResultState.CompletedWithResult, result.State);
        Assert.Equal(DurableOperationStatus.Completed, result.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"), result.UpdatedAtUtc);
        Assert.NotNull(result.DiagnosticContext);
        Assert.Equal("fulfillment", result.DiagnosticContext.ModuleName);
        Assert.Equal("fulfillment.order.reserve.v1", result.DiagnosticContext.MessageTypeName);
        Assert.Equal("receive.fulfillment.order.reserve.v1", result.DiagnosticContext.HandlerIdentity);
        Assert.Null(result.DeserializationFailure);
        Assert.Equal(new ReserveOrderResult(new DurableOrderId("A-100"), Accepted: true), result.Result);
    }

    [Theory]
    [InlineData(DurableOperationStatus.Pending, DurableOperationResultState.Pending)]
    [InlineData(DurableOperationStatus.Running, DurableOperationResultState.Running)]
    [Trait("Category", "Unit")]
    public async Task GetResultAsync_WhenOperationIsNotTerminal_ReturnsStateWithoutResult(
        DurableOperationStatus operationStatus,
        DurableOperationResultState expectedState)
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                operationStatus,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(durableOperationId);

        Assert.True(result.IsKnown);
        Assert.False(result.IsCompleted);
        Assert.False(result.IsTerminal);
        Assert.False(result.HasResult);
        Assert.Equal(expectedState, result.State);
        Assert.Equal(operationStatus, result.Status);
        Assert.Null(result.DeserializationFailure);
        Assert.Null(result.Result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResultAsync_WhenOperationCompletedWithoutResultPayload_ReturnsCompletedWithoutResult()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(durableOperationId);

        Assert.True(result.IsKnown);
        Assert.True(result.IsCompleted);
        Assert.True(result.IsTerminal);
        Assert.False(result.HasResult);
        Assert.Equal(DurableOperationResultState.CompletedWithoutResult, result.State);
        Assert.Equal(DurableOperationStatus.Completed, result.Status);
        Assert.Null(result.DeserializationFailure);
        Assert.Null(result.Result);
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
        Assert.Equal(DurableOperationResultState.Failed, result.State);
        Assert.Equal(DurableOperationStatus.Failed, result.Status);
        Assert.Equal("handler failed", result.FailureReason);
        Assert.Null(result.DeserializationFailure);
        Assert.Null(result.Result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResultAsync_WhenModuleHintIsProvided_ReadsOnlyHintedModuleStore()
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
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"),
                """{"orderId":"payload-A-100","accepted":true}"""),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            salesStore,
            fulfillmentStore);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(
                durableOperationId,
                " sales ");

        Assert.Equal(DurableOperationResultState.Pending, result.State);
        Assert.Equal(1, salesStore.ReadCount);
        Assert.Equal(0, fulfillmentStore.ReadCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResultAsync_WhenOperationCancelled_ReturnsTerminalCancelledWithoutResult()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Cancelled,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"),
                failureReason: "caller cancelled"),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(durableOperationId);

        Assert.True(result.IsKnown);
        Assert.False(result.IsCompleted);
        Assert.True(result.IsTerminal);
        Assert.False(result.HasResult);
        Assert.Equal(DurableOperationResultState.Cancelled, result.State);
        Assert.Equal(DurableOperationStatus.Cancelled, result.Status);
        Assert.Equal("caller cancelled", result.FailureReason);
        Assert.Null(result.DeserializationFailure);
        Assert.Null(result.Result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResultAsync_WhenOperationCompletedWithInvalidResultPayload_ReturnsDeserializationFailure()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"),
                """{"orderId":{"unexpected":true},"accepted":true}""",
                diagnosticContext: new DurableOperationDiagnosticContext(
                    "fulfillment",
                    "fulfillment.order.reserve.v1",
                    "receive.fulfillment.order.reserve.v1")),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(durableOperationId);

        Assert.True(result.IsKnown);
        Assert.True(result.IsCompleted);
        Assert.True(result.IsTerminal);
        Assert.False(result.HasResult);
        Assert.Equal(DurableOperationResultState.ResultDeserializationFailed, result.State);
        Assert.Equal(DurableOperationStatus.Completed, result.Status);
        Assert.NotNull(result.DiagnosticContext);
        Assert.Equal("fulfillment", result.DiagnosticContext.ModuleName);
        Assert.Equal("fulfillment.order.reserve.v1", result.DiagnosticContext.MessageTypeName);
        Assert.Equal("receive.fulfillment.order.reserve.v1", result.DiagnosticContext.HandlerIdentity);
        Assert.Null(result.Result);

        DurableOperationResultDeserializationFailure failure =
            Assert.IsType<DurableOperationResultDeserializationFailure>(
                result.DeserializationFailure);
        Assert.Equal(durableOperationId, failure.DurableOperationId);
        Assert.Equal(typeof(ReserveOrderResult).FullName, failure.ResultTypeName);
        Assert.Contains(durableOperationId.ToString(), failure.Message);
        Assert.Contains(typeof(ReserveOrderResult).FullName!, failure.Message);
        Assert.Contains("fulfillment", failure.Message);
        Assert.Contains("fulfillment.order.reserve.v1", failure.Message);
        Assert.Contains("receive.fulfillment.order.reserve.v1", failure.Message);
        Assert.Equal(typeof(System.Text.Json.JsonException).FullName, failure.ExceptionTypeName);
        Assert.NotNull(failure.DiagnosticContext);
        Assert.Equal("fulfillment", failure.DiagnosticContext.ModuleName);
        Assert.Equal("fulfillment.order.reserve.v1", failure.DiagnosticContext.MessageTypeName);
        Assert.Equal("receive.fulfillment.order.reserve.v1", failure.DiagnosticContext.HandlerIdentity);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetResultAsync_WhenInvalidResultPayloadHasNoDiagnosticContext_ReturnsUsefulDeserializationFailure()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"),
                """{"orderId":{"unexpected":true},"accepted":true}"""),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .GetResultAsync<ReserveOrderResult>(durableOperationId);

        DurableOperationResultDeserializationFailure failure =
            Assert.IsType<DurableOperationResultDeserializationFailure>(
                result.DeserializationFailure);
        Assert.Contains(durableOperationId.ToString(), failure.Message);
        Assert.Contains(typeof(ReserveOrderResult).FullName!, failure.Message);
        Assert.Null(result.DiagnosticContext);
        Assert.Null(failure.DiagnosticContext);
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
        Assert.Equal(DurableOperationResultState.CompletedWithResult, result.State);
        Assert.Equal(new ReserveOrderResult(new DurableOrderId("A-100"), Accepted: true), result.Result);
        Assert.True(store.ReadCount >= 2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task WaitForResultAsync_WhenOperationIsFailed_ReturnsFailureWithoutPollingForever()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new SequencedModuleOperationStateStore(
            "fulfillment",
            new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Failed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"),
                failureReason: "expired"));
        await using ServiceProvider serviceProvider = CreateServiceProvider(store);

        DurableOperationResult<ReserveOrderResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .WaitForResultAsync<ReserveOrderResult>(
                durableOperationId,
                "fulfillment",
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(1));

        Assert.True(result.IsTerminal);
        Assert.Equal(DurableOperationResultState.Failed, result.State);
        Assert.Equal("expired", result.FailureReason);
        Assert.Equal(1, store.ReadCount);
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
