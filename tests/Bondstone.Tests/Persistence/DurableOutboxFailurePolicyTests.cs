using Bondstone.Messaging;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableOutboxFailurePolicyTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenMaxAttemptsIsNotPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DurableOutboxFailurePolicy(maxAttempts: 0));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenRetryDelayIsNegative_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new DurableOutboxFailurePolicy(
                retryDelays: [TimeSpan.Zero, TimeSpan.FromSeconds(-1)]));

        Assert.Equal("retryDelays", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DecideFailure_WhenAttemptCanRetry_ReturnsRetryDecision()
    {
        var policy = new DurableOutboxFailurePolicy(
            maxAttempts: 3,
            retryDelays: [TimeSpan.Zero, TimeSpan.FromSeconds(10)]);
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:00+00:00");

        DurableOutboxFailureDecision decision = policy.DecideFailure(
            CreateRecord(attemptCount: 2),
            " transport failed ",
            failedAtUtc);

        Assert.Equal(DurableOutboxFailureDecisionKind.Retry, decision.Kind);
        Assert.Equal("transport failed", decision.FailureReason);
        Assert.Equal(failedAtUtc.AddSeconds(10), decision.NextAttemptAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DecideFailure_WhenAttemptExceedsRetryDelayList_UsesLastDelay()
    {
        var policy = new DurableOutboxFailurePolicy(
            maxAttempts: 5,
            retryDelays: [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10)]);
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:00+00:00");

        DurableOutboxFailureDecision decision = policy.DecideFailure(
            CreateRecord(attemptCount: 4),
            "transport failed",
            failedAtUtc);

        Assert.Equal(DurableOutboxFailureDecisionKind.Retry, decision.Kind);
        Assert.Equal(failedAtUtc.AddSeconds(10), decision.NextAttemptAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DecideFailure_WhenNoRetryDelaysAreConfigured_RetriesImmediately()
    {
        var policy = new DurableOutboxFailurePolicy(
            maxAttempts: 3,
            retryDelays: []);
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:00+00:00");

        DurableOutboxFailureDecision decision = policy.DecideFailure(
            CreateRecord(attemptCount: 1),
            "transport failed",
            failedAtUtc);

        Assert.Equal(DurableOutboxFailureDecisionKind.Retry, decision.Kind);
        Assert.Equal(failedAtUtc, decision.NextAttemptAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DecideFailure_WhenMaxAttemptsReached_ReturnsTerminalFailureDecision()
    {
        var policy = new DurableOutboxFailurePolicy(maxAttempts: 3);
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-05T00:00:00+00:00");

        DurableOutboxFailureDecision decision = policy.DecideFailure(
            CreateRecord(attemptCount: 3),
            "poison message",
            failedAtUtc);

        Assert.Equal(DurableOutboxFailureDecisionKind.TerminalFailure, decision.Kind);
        Assert.Equal("poison message", decision.FailureReason);
        Assert.Null(decision.NextAttemptAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DecideFailure_WhenRecordIsNotProcessing_Throws()
    {
        var policy = new DurableOutboxFailurePolicy();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => policy.DecideFailure(
                CreateRecord(DurableOutboxStatus.Pending, attemptCount: 0),
                "transport failed",
                DateTimeOffset.Parse("2026-06-05T00:00:00+00:00")));

        Assert.Equal("record", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DecideFailure_WhenProcessingRecordHasNoAttempt_Throws()
    {
        var policy = new DurableOutboxFailurePolicy();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => policy.DecideFailure(
                CreateRecord(attemptCount: 0),
                "transport failed",
                DateTimeOffset.Parse("2026-06-05T00:00:00+00:00")));

        Assert.Equal("record", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DecideFailure_WhenFailedAtHasNonUtcOffset_Throws()
    {
        var policy = new DurableOutboxFailurePolicy();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => policy.DecideFailure(
                CreateRecord(attemptCount: 1),
                "transport failed",
                DateTimeOffset.Parse("2026-06-05T00:00:00+02:00")));

        Assert.Equal("failedAtUtc", exception.ParamName);
    }

    private static DurableOutboxRecord CreateRecord(
        DurableOutboxStatus status = DurableOutboxStatus.Processing,
        int attemptCount = 1)
    {
        var dispatchState = new DurableOutboxDispatchState(
            status,
            attemptCount,
            claimedBy: status == DurableOutboxStatus.Processing ? "dispatcher-1" : null,
            claimedUntilUtc: status == DurableOutboxStatus.Processing
                ? DateTimeOffset.Parse("2026-06-05T00:05:00+00:00")
                : null);

        return new DurableOutboxRecord(
            CreateEnvelope(),
            DateTimeOffset.Parse("2026-06-05T00:00:00+00:00"),
            dispatchState);
    }

    private static DurableMessageEnvelope CreateEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "sales.customer.register.v1",
            "sales",
            "billing",
            "{}",
            DateTimeOffset.Parse("2026-06-05T00:00:00+00:00"));
    }
}
