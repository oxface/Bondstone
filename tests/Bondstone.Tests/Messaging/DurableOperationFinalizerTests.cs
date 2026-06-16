using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableOperationFinalizerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkFailedAsync_WhenOperationIsUnknown_WritesFailedStateToModuleStore()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-16T12:00:00+00:00");
        var store = new CapturingModuleOperationStateStore("fulfillment");
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            failedAtUtc,
            store);

        DurableOperationFinalizationResult result = await serviceProvider
            .GetRequiredService<IDurableOperationFinalizer>()
            .MarkFailedAsync(
                "fulfillment",
                durableOperationId,
                "expired before receive");

        Assert.True(result.WasFinalized);
        Assert.True(result.IsTerminal);
        Assert.Equal(DurableOperationStatus.Failed, result.Status);
        Assert.Equal(durableOperationId, result.DurableOperationId);

        DurableOperationState state = Assert.Single(store.SavedStates);
        Assert.Equal(durableOperationId, state.DurableOperationId);
        Assert.Equal(DurableOperationStatus.Failed, state.Status);
        Assert.Equal(failedAtUtc, state.UpdatedAtUtc);
        Assert.Equal("expired before receive", state.FailureReason);
        Assert.Same(state, result.State);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkFailedAsync_WhenResultReaderWaits_ProducesTerminalFailedResult()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var store = new CapturingModuleOperationStateStore("fulfillment");
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            DateTimeOffset.Parse("2026-06-16T12:00:00+00:00"),
            store);

        await serviceProvider
            .GetRequiredService<IDurableOperationFinalizer>()
            .MarkFailedAsync(
                "fulfillment",
                durableOperationId,
                "expired before completion");

        DurableOperationResult<TestResult> result = await serviceProvider
            .GetRequiredService<IDurableOperationResultReader>()
            .WaitForResultAsync<TestResult>(
                durableOperationId,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(1));

        Assert.True(result.IsTerminal);
        Assert.False(result.IsCompleted);
        Assert.Equal(DurableOperationResultState.Failed, result.State);
        Assert.Equal("expired before completion", result.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkCancelledAsync_WhenOperationIsPending_PreservesExistingDiagnosticContext()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        DateTimeOffset cancelledAtUtc = DateTimeOffset.Parse("2026-06-16T12:00:00+00:00");
        var diagnosticContext = new DurableOperationDiagnosticContext(
            "fulfillment",
            "fulfillment.order.reserve.v1",
            "reserve.handler.v1");
        var store = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-16T11:00:00+00:00"),
                diagnosticContext: diagnosticContext),
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            cancelledAtUtc,
            store);

        DurableOperationFinalizationResult result = await serviceProvider
            .GetRequiredService<IDurableOperationFinalizer>()
            .MarkCancelledAsync(
                "fulfillment",
                durableOperationId,
                "caller abandoned workflow");

        Assert.True(result.WasFinalized);
        Assert.Equal(DurableOperationStatus.Cancelled, result.Status);

        DurableOperationState state = Assert.Single(store.SavedStates);
        Assert.Equal(cancelledAtUtc, state.UpdatedAtUtc);
        Assert.Equal("caller abandoned workflow", state.FailureReason);
        Assert.Same(diagnosticContext, state.DiagnosticContext);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkFailedAsync_WhenOperationIsAlreadyTerminal_DoesNotOverwriteExistingState()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        DurableOperationState completedState = new(
            durableOperationId,
            DurableOperationStatus.Completed,
            DateTimeOffset.Parse("2026-06-16T11:00:00+00:00"),
            resultPayload: "{}");
        var store = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = completedState,
        };
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            DateTimeOffset.Parse("2026-06-16T12:00:00+00:00"),
            store);

        DurableOperationFinalizationResult result = await serviceProvider
            .GetRequiredService<IDurableOperationFinalizer>()
            .MarkFailedAsync(
                "fulfillment",
                durableOperationId,
                "late timeout");

        Assert.False(result.WasFinalized);
        Assert.True(result.IsTerminal);
        Assert.Equal(DurableOperationStatus.Completed, result.Status);
        Assert.Same(completedState, result.State);
        Assert.Empty(store.SavedStates);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkFailedAsync_WhenModuleOperationStoreIsMissing_ThrowsClearError()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            DateTimeOffset.Parse("2026-06-16T12:00:00+00:00"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await serviceProvider
                .GetRequiredService<IDurableOperationFinalizer>()
                .MarkFailedAsync(
                    "fulfillment",
                    durableOperationId,
                    "expired"));

        Assert.Contains(nameof(IDurableOperationStateStore), exception.Message, StringComparison.Ordinal);
        Assert.Contains("explicit operation finalization", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("")]
    [InlineData(" ")]
    public async Task MarkFailedAsync_WhenReasonIsBlank_Throws(
        string reason)
    {
        var store = new CapturingModuleOperationStateStore("fulfillment");
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            DateTimeOffset.Parse("2026-06-16T12:00:00+00:00"),
            store);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await serviceProvider
                .GetRequiredService<IDurableOperationFinalizer>()
                .MarkFailedAsync(
                    "fulfillment",
                    Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766"),
                    reason));
    }

    private static ServiceProvider CreateServiceProvider(
        DateTimeOffset utcNow,
        params CapturingModuleOperationStateStore[] stores)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(utcNow));

        services.AddBondstone(bondstone =>
        {
            foreach (CapturingModuleOperationStateStore store in stores)
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

    private sealed class CapturingModuleOperationStateStore(string moduleName)
        : IDurableOperationStateStore
    {
        public string ModuleName { get; } = moduleName;

        public DurableOperationState? State { get; set; }

        public List<DurableOperationState> SavedStates { get; } = [];

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
            State = state;
            SavedStates.Add(state);
            return ValueTask.CompletedTask;
        }
    }

    private sealed record TestResult(string Value);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
