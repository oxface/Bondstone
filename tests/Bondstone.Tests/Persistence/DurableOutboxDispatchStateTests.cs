using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableOutboxDispatchStateTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_CarriesDispatchState()
    {
        DateTimeOffset nextAttemptAtUtc = DateTimeOffset.Parse("2026-06-04T00:01:00+00:00");
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-04T00:00:30+00:00");

        var state = new DurableOutboxDispatchState(
            DurableOutboxStatus.Failed,
            attemptCount: 2,
            nextAttemptAtUtc,
            failedAtUtc: failedAtUtc,
            failureReason: " failed once ");

        Assert.Equal(DurableOutboxStatus.Failed, state.Status);
        Assert.Equal(2, state.AttemptCount);
        Assert.Equal(nextAttemptAtUtc, state.NextAttemptAtUtc);
        Assert.Null(state.DispatchedAtUtc);
        Assert.Equal(failedAtUtc, state.FailedAtUtc);
        Assert.Equal(" failed once ", state.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Pending_ReturnsPendingStateWithoutAttempts()
    {
        DurableOutboxDispatchState state = DurableOutboxDispatchState.Pending;

        Assert.Equal(DurableOutboxStatus.Pending, state.Status);
        Assert.Equal(0, state.AttemptCount);
        Assert.Null(state.NextAttemptAtUtc);
        Assert.Null(state.DispatchedAtUtc);
        Assert.Null(state.FailedAtUtc);
        Assert.Null(state.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenFailureReasonIsWhitespace_StoresNull()
    {
        var state = new DurableOutboxDispatchState(
            DurableOutboxStatus.DeadLettered,
            attemptCount: 5,
            failureReason: " ");

        Assert.Null(state.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStatusIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateState(status: (DurableOutboxStatus)999));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenAttemptCountIsNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateState(attemptCount: -1));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenTimestampIsDefault_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(nextAttemptAtUtc: DateTimeOffset.MinValue));

        Assert.Equal("nextAttemptAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenTimestampHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(dispatchedAtUtc: DateTimeOffset.Parse("2026-06-04T00:00:00+02:00")));

        Assert.Equal("dispatchedAtUtc", exception.ParamName);
    }

    private static DurableOutboxDispatchState CreateState(
        DurableOutboxStatus status = DurableOutboxStatus.Pending,
        int attemptCount = 0,
        DateTimeOffset? nextAttemptAtUtc = null,
        DateTimeOffset? dispatchedAtUtc = null)
    {
        return new DurableOutboxDispatchState(
            status,
            attemptCount,
            nextAttemptAtUtc,
            dispatchedAtUtc);
    }
}
