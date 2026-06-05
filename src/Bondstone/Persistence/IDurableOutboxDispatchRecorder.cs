namespace Bondstone.Persistence;

public interface IDurableOutboxDispatchRecorder
{
    ValueTask<bool> MarkDispatchedAsync(
        Guid messageId,
        string claimedBy,
        DateTimeOffset dispatchedAtUtc,
        CancellationToken cancellationToken = default);

    ValueTask<bool> ScheduleRetryAsync(
        Guid messageId,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        DateTimeOffset nextAttemptAtUtc,
        CancellationToken cancellationToken = default);

    ValueTask<bool> MarkDeadLetteredAsync(
        Guid messageId,
        string claimedBy,
        string failureReason,
        DateTimeOffset failedAtUtc,
        CancellationToken cancellationToken = default);
}
