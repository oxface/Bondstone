using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableIncomingInboxFailureDecisionTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Retry_WhenValuesAreValid_CreatesRetryDecision()
    {
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-17T00:00:00+00:00");
        DateTimeOffset nextAttemptAtUtc = DateTimeOffset.Parse("2026-06-17T00:00:10+00:00");

        DurableIncomingInboxFailureDecision decision = DurableIncomingInboxFailureDecision.Retry(
            " receive failed ",
            failedAtUtc,
            nextAttemptAtUtc);

        Assert.Equal(DurableIncomingInboxFailureDecisionKind.Retry, decision.Kind);
        Assert.True(decision.ShouldRetry);
        Assert.False(decision.ShouldTerminalFail);
        Assert.Equal("receive failed", decision.FailureReason);
        Assert.Equal(failedAtUtc, decision.FailedAtUtc);
        Assert.Equal(nextAttemptAtUtc, decision.NextAttemptAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TerminalFailure_WhenValuesAreValid_CreatesTerminalFailureDecision()
    {
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-17T00:00:00+00:00");

        DurableIncomingInboxFailureDecision decision =
            DurableIncomingInboxFailureDecision.TerminalFailure(
                "poison receive",
                failedAtUtc);

        Assert.Equal(DurableIncomingInboxFailureDecisionKind.TerminalFailure, decision.Kind);
        Assert.False(decision.ShouldRetry);
        Assert.True(decision.ShouldTerminalFail);
        Assert.Equal("poison receive", decision.FailureReason);
        Assert.Equal(failedAtUtc, decision.FailedAtUtc);
        Assert.Null(decision.NextAttemptAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Retry_WhenNextAttemptIsBeforeFailure_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => DurableIncomingInboxFailureDecision.Retry(
                "failed",
                DateTimeOffset.Parse("2026-06-17T00:00:10+00:00"),
                DateTimeOffset.Parse("2026-06-17T00:00:09+00:00")));

        Assert.Equal("nextAttemptAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TerminalFailure_WhenFailureReasonIsBlank_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => DurableIncomingInboxFailureDecision.TerminalFailure(
                " ",
                DateTimeOffset.Parse("2026-06-17T00:00:00+00:00")));

        Assert.Equal("failureReason", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TerminalFailure_WhenTimestampHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => DurableIncomingInboxFailureDecision.TerminalFailure(
                "failed",
                DateTimeOffset.Parse("2026-06-17T00:00:00+02:00")));

        Assert.Equal("failedAtUtc", exception.ParamName);
    }
}
