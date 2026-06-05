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
            failureReason: " failed once ",
            claimedBy: " dispatcher-1 ",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-04T00:02:00+00:00"));

        Assert.Equal(DurableOutboxStatus.Failed, state.Status);
        Assert.Equal(2, state.AttemptCount);
        Assert.Equal(nextAttemptAtUtc, state.NextAttemptAtUtc);
        Assert.Null(state.DispatchedAtUtc);
        Assert.Equal(failedAtUtc, state.FailedAtUtc);
        Assert.Equal(" failed once ", state.FailureReason);
        Assert.Equal("dispatcher-1", state.ClaimedBy);
        Assert.Equal(DateTimeOffset.Parse("2026-06-04T00:02:00+00:00"), state.ClaimedUntilUtc);
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
        Assert.Null(state.ClaimedBy);
        Assert.Null(state.ClaimedUntilUtc);
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

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenClaimedByIsSetWithoutClaimedUntil_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(claimedBy: "worker-1"));

        Assert.Equal("claimedUntilUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenClaimedUntilIsSetWithoutClaimedBy_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(claimedUntilUtc: DateTimeOffset.Parse("2026-06-04T00:02:00+00:00")));

        Assert.Equal("claimedBy", exception.ParamName);
    }

    private static DurableOutboxDispatchState CreateState(
        DurableOutboxStatus status = DurableOutboxStatus.Pending,
        int attemptCount = 0,
        DateTimeOffset? nextAttemptAtUtc = null,
        DateTimeOffset? dispatchedAtUtc = null,
        string? claimedBy = null,
        DateTimeOffset? claimedUntilUtc = null)
    {
        return new DurableOutboxDispatchState(
            status,
            attemptCount,
            nextAttemptAtUtc,
            dispatchedAtUtc,
            claimedBy: claimedBy,
            claimedUntilUtc: claimedUntilUtc);
    }
}
