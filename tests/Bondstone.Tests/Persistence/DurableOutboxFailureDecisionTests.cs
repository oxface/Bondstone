using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableOutboxFailureDecisionTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Retry_WhenValuesAreValid_CreatesRetryDecision()
    {
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:00+00:00");
        DateTimeOffset nextAttemptAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:10+00:00");

        DurableOutboxFailureDecision decision = DurableOutboxFailureDecision.Retry(
            " transport failed ",
            failedAtUtc,
            nextAttemptAtUtc);

        Assert.Equal(DurableOutboxFailureDecisionKind.Retry, decision.Kind);
        Assert.True(decision.ShouldRetry);
        Assert.False(decision.ShouldTerminalFail);
        Assert.Equal("transport failed", decision.FailureReason);
        Assert.Equal(failedAtUtc, decision.FailedAtUtc);
        Assert.Equal(nextAttemptAtUtc, decision.NextAttemptAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TerminalFailure_WhenValuesAreValid_CreatesTerminalFailureDecision()
    {
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:00+00:00");

        DurableOutboxFailureDecision decision = DurableOutboxFailureDecision.TerminalFailure(
            "poison message",
            failedAtUtc);

        Assert.Equal(DurableOutboxFailureDecisionKind.TerminalFailure, decision.Kind);
        Assert.False(decision.ShouldRetry);
        Assert.True(decision.ShouldTerminalFail);
        Assert.Equal("poison message", decision.FailureReason);
        Assert.Equal(failedAtUtc, decision.FailedAtUtc);
        Assert.Null(decision.NextAttemptAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Retry_WhenNextAttemptIsBeforeFailure_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => DurableOutboxFailureDecision.Retry(
                "failed",
                DateTimeOffset.Parse("2026-06-05T00:00:10+00:00"),
                DateTimeOffset.Parse("2026-06-05T00:00:09+00:00")));

        Assert.Equal("nextAttemptAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TerminalFailure_WhenFailureReasonIsBlank_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => DurableOutboxFailureDecision.TerminalFailure(
                " ",
                DateTimeOffset.Parse("2026-06-05T00:00:00+00:00")));

        Assert.Equal("failureReason", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TerminalFailure_WhenTimestampHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => DurableOutboxFailureDecision.TerminalFailure(
                "failed",
                DateTimeOffset.Parse("2026-06-05T00:00:00+02:00")));

        Assert.Equal("failedAtUtc", exception.ParamName);
    }
}
