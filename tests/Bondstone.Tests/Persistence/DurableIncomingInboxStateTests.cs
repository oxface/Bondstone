using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableIncomingInboxStateTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Pending_ReturnsPendingStateWithoutAttempts()
    {
        DurableIncomingInboxState state = DurableIncomingInboxState.Pending;

        Assert.Equal(DurableIncomingInboxStatus.Pending, state.Status);
        Assert.Equal(0, state.AttemptCount);
        Assert.Null(state.NextAttemptAtUtc);
        Assert.Null(state.ProcessedAtUtc);
        Assert.Null(state.FailedAtUtc);
        Assert.Null(state.FailureReason);
        Assert.Null(state.ClaimedBy);
        Assert.Null(state.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenProcessing_CarriesClaimState()
    {
        DateTimeOffset claimedUntilUtc = DateTimeOffset.Parse("2026-06-17T00:05:00+00:00");

        var state = new DurableIncomingInboxState(
            DurableIncomingInboxStatus.Processing,
            attemptCount: 1,
            claimedBy: " worker-1 ",
            claimedUntilUtc: claimedUntilUtc);

        Assert.Equal(DurableIncomingInboxStatus.Processing, state.Status);
        Assert.Equal(1, state.AttemptCount);
        Assert.Equal("worker-1", state.ClaimedBy);
        Assert.Equal(claimedUntilUtc, state.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenProcessed_CarriesProcessedTimestamp()
    {
        DateTimeOffset processedAtUtc = DateTimeOffset.Parse("2026-06-17T00:06:00+00:00");

        var state = new DurableIncomingInboxState(
            DurableIncomingInboxStatus.Processed,
            attemptCount: 1,
            processedAtUtc: processedAtUtc);

        Assert.Equal(DurableIncomingInboxStatus.Processed, state.Status);
        Assert.Equal(processedAtUtc, state.ProcessedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenRetryScheduled_CarriesRetryState()
    {
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-17T00:06:00+00:00");
        DateTimeOffset nextAttemptAtUtc = DateTimeOffset.Parse("2026-06-17T00:07:00+00:00");

        var state = new DurableIncomingInboxState(
            DurableIncomingInboxStatus.RetryScheduled,
            attemptCount: 2,
            nextAttemptAtUtc,
            failedAtUtc: failedAtUtc,
            failureReason: " receive failed ");

        Assert.Equal(DurableIncomingInboxStatus.RetryScheduled, state.Status);
        Assert.Equal(2, state.AttemptCount);
        Assert.Equal(nextAttemptAtUtc, state.NextAttemptAtUtc);
        Assert.Equal(failedAtUtc, state.FailedAtUtc);
        Assert.Equal(" receive failed ", state.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenTerminalFailed_CarriesTerminalFailureState()
    {
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-17T00:06:00+00:00");

        var state = new DurableIncomingInboxState(
            DurableIncomingInboxStatus.TerminalFailed,
            attemptCount: 5,
            failedAtUtc: failedAtUtc,
            failureReason: "poison receive");

        Assert.Equal(DurableIncomingInboxStatus.TerminalFailed, state.Status);
        Assert.Equal(failedAtUtc, state.FailedAtUtc);
        Assert.Equal("poison receive", state.FailureReason);
        Assert.Null(state.NextAttemptAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStatusIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateState(status: (DurableIncomingInboxStatus)999));
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
            () => CreateState(processedAtUtc: DateTimeOffset.MinValue));

        Assert.Equal("processedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenTimestampHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(processedAtUtc: DateTimeOffset.Parse("2026-06-17T00:06:00+02:00")));

        Assert.Equal("processedAtUtc", exception.ParamName);
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
            () => CreateState(claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00")));

        Assert.Equal("claimedBy", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenProcessingStatusOmitsClaim_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(status: DurableIncomingInboxStatus.Processing));

        Assert.Equal("claimedBy", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenPendingStatusHasClaim_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00")));

        Assert.Equal("claimedBy", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenProcessedStatusHasClaim_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(
                status: DurableIncomingInboxStatus.Processed,
                processedAtUtc: DateTimeOffset.Parse("2026-06-17T00:06:00+00:00"),
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00")));

        Assert.Equal("claimedBy", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenRetryScheduledStatusHasClaim_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(
                status: DurableIncomingInboxStatus.RetryScheduled,
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:06:00+00:00"),
                nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-17T00:07:00+00:00"),
                failureReason: "failed",
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00")));

        Assert.Equal("claimedBy", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenTerminalFailedStatusHasClaim_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(
                status: DurableIncomingInboxStatus.TerminalFailed,
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:06:00+00:00"),
                failureReason: "failed",
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00")));

        Assert.Equal("claimedBy", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenProcessedStatusOmitsProcessedAt_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(status: DurableIncomingInboxStatus.Processed));

        Assert.Equal("processedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenPendingStatusHasFailureReason_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(failureReason: "failed"));

        Assert.Equal("failureReason", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenRetryScheduledStatusOmitsFailureReason_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(
                status: DurableIncomingInboxStatus.RetryScheduled,
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:06:00+00:00"),
                nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-17T00:07:00+00:00")));

        Assert.Equal("failureReason", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenRetryScheduledStatusOmitsNextAttempt_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(
                status: DurableIncomingInboxStatus.RetryScheduled,
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:06:00+00:00"),
                failureReason: "failed"));

        Assert.Equal("nextAttemptAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenNextAttemptIsEarlierThanFailedAt_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(
                status: DurableIncomingInboxStatus.RetryScheduled,
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:06:00+00:00"),
                nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-17T00:05:59+00:00"),
                failureReason: "failed"));

        Assert.Equal("nextAttemptAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenTerminalFailedStatusHasNextAttempt_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(
                status: DurableIncomingInboxStatus.TerminalFailed,
                failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:06:00+00:00"),
                nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-17T00:07:00+00:00"),
                failureReason: "failed"));

        Assert.Equal("nextAttemptAtUtc", exception.ParamName);
    }

    private static DurableIncomingInboxState CreateState(
        DurableIncomingInboxStatus status = DurableIncomingInboxStatus.Pending,
        int attemptCount = 0,
        DateTimeOffset? nextAttemptAtUtc = null,
        DateTimeOffset? processedAtUtc = null,
        DateTimeOffset? failedAtUtc = null,
        string? failureReason = null,
        string? claimedBy = null,
        DateTimeOffset? claimedUntilUtc = null)
    {
        return new DurableIncomingInboxState(
            status,
            attemptCount,
            nextAttemptAtUtc,
            processedAtUtc,
            failedAtUtc,
            failureReason,
            claimedBy,
            claimedUntilUtc);
    }
}
