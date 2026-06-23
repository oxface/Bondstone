namespace Bondstone.Persistence;

public interface IDurableIncomingInboxOutcomeRecorder
{
    ValueTask<bool> MarkProcessedAsync(
        DurableIncomingInboxKey key,
        string claimedBy,
        DateTimeOffset processedAtUtc,
        CancellationToken ct = default);

    ValueTask<bool> ScheduleRetryAsync(
        DurableIncomingInboxKey key,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset nextAttemptAtUtc,
        CancellationToken ct = default);

    ValueTask<bool> MarkTerminalFailedAsync(
        DurableIncomingInboxKey key,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        CancellationToken ct = default);
}
