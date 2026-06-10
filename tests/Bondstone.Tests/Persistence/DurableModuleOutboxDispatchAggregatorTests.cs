using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableModuleOutboxDispatchAggregatorTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenModuleDispatchersClaimRows_SharesMaxCountSequentiallyAndAggregates()
    {
        var sales = new RecordingModuleOutboxDispatcher(
            "sales",
            new DurableOutboxDispatchResult(3, 2, 1, 0, 0));
        var fulfillment = new RecordingModuleOutboxDispatcher(
            "fulfillment",
            new DurableOutboxDispatchResult(2, 1, 0, 1, 0));
        var aggregator = new DurableModuleOutboxDispatchAggregator(
            [sales, fulfillment]);

        DurableOutboxDispatchResult result = await aggregator.DispatchAsync(
            " worker-1 ",
            TimeSpan.FromMinutes(2),
            maxCount: 5);

        Assert.Equal(5, result.ClaimedCount);
        Assert.Equal(3, result.DispatchedCount);
        Assert.Equal(1, result.RetryScheduledCount);
        Assert.Equal(1, result.TerminalFailedCount);
        Assert.Equal(0, result.StaleCount);

        DispatchCall salesCall = Assert.Single(sales.Calls);
        Assert.Equal("worker-1", salesCall.ClaimedBy);
        Assert.Equal(TimeSpan.FromMinutes(2), salesCall.LeaseDuration);
        Assert.Equal(5, salesCall.MaxCount);

        DispatchCall fulfillmentCall = Assert.Single(fulfillment.Calls);
        Assert.Equal("worker-1", fulfillmentCall.ClaimedBy);
        Assert.Equal(TimeSpan.FromMinutes(2), fulfillmentCall.LeaseDuration);
        Assert.Equal(2, fulfillmentCall.MaxCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenSharedBudgetIsExhausted_SkipsRemainingModules()
    {
        var sales = new RecordingModuleOutboxDispatcher(
            "sales",
            new DurableOutboxDispatchResult(5, 5, 0, 0, 0));
        var fulfillment = new RecordingModuleOutboxDispatcher(
            "fulfillment",
            new DurableOutboxDispatchResult(1, 1, 0, 0, 0));
        var aggregator = new DurableModuleOutboxDispatchAggregator(
            [sales, fulfillment]);

        DurableOutboxDispatchResult result = await aggregator.DispatchAsync(
            "worker-1",
            TimeSpan.FromMinutes(2),
            maxCount: 5);

        Assert.Equal(5, result.ClaimedCount);
        Assert.Single(sales.Calls);
        Assert.Empty(fulfillment.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenModuleDispatcherFails_PropagatesAndStopsBatch()
    {
        var sales = new RecordingModuleOutboxDispatcher(
            "sales",
            new DurableOutboxDispatchResult(1, 1, 0, 0, 0));
        var fulfillment = new RecordingModuleOutboxDispatcher(
            "fulfillment",
            new InvalidOperationException("database unavailable"));
        var shipping = new RecordingModuleOutboxDispatcher(
            "shipping",
            new DurableOutboxDispatchResult(1, 1, 0, 0, 0));
        var aggregator = new DurableModuleOutboxDispatchAggregator(
            [sales, fulfillment, shipping]);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await aggregator.DispatchAsync(
                "worker-1",
                TimeSpan.FromMinutes(2),
                maxCount: 5));

        Assert.Equal("database unavailable", exception.Message);
        Assert.Single(sales.Calls);
        Assert.Single(fulfillment.Calls);
        Assert.Empty(shipping.Calls);
    }

    private sealed class RecordingModuleOutboxDispatcher(
        string moduleName,
        object outcome)
        : IDurableModuleOutboxDispatcher
    {
        public string ModuleName { get; } = moduleName;

        public List<DispatchCall> Calls { get; } = [];

        public ValueTask<DurableOutboxDispatchResult> DispatchAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken ct = default)
        {
            Calls.Add(new DispatchCall(claimedBy, leaseDuration, maxCount));

            if (outcome is Exception exception)
            {
                throw exception;
            }

            return ValueTask.FromResult((DurableOutboxDispatchResult)outcome);
        }
    }

    private sealed record DispatchCall(
        string ClaimedBy,
        TimeSpan LeaseDuration,
        int MaxCount);
}
