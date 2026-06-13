using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableOutboxDispatchResultTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_CarriesCounts()
    {
        var result = new DurableOutboxDispatchResult(
            claimedCount: 5,
            dispatchedCount: 2,
            retryScheduledCount: 1,
            terminalFailedCount: 1,
            staleCount: 1);

        Assert.Equal(5, result.ClaimedCount);
        Assert.Equal(2, result.DispatchedCount);
        Assert.Equal(1, result.RetryScheduledCount);
        Assert.Equal(1, result.TerminalFailedCount);
        Assert.Equal(1, result.StaleCount);
        Assert.Equal(4, result.CompletedCount);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenCountIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DurableOutboxDispatchResult(
                claimedCount: -1,
                dispatchedCount: 0,
                retryScheduledCount: 0,
                terminalFailedCount: 0,
                staleCount: 0));
    }
}
