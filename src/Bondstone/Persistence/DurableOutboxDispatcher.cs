using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableOutboxDispatcher(
    IDurableOutboxClaimer claimer,
    IDurableOutboxLeaseRenewer leaseRenewer,
    IDurableOutboxTransport transport,
    IDurableOutboxFailurePolicy failurePolicy,
    IDurableOutboxDispatchRecorder dispatchRecorder,
    TimeProvider? timeProvider = null)
    : IDurableOutboxDispatcher
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async ValueTask<DurableOutboxDispatchResult> DispatchAsync(
        string claimedBy,
        TimeSpan leaseDuration,
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        string normalizedClaimedBy = claimedBy.NormalizeRequired(nameof(claimedBy), "Claim owner");

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration),
                leaseDuration,
                "Lease duration must be positive.");
        }

        if (maxCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCount),
                maxCount,
                "Maximum dispatch count must be positive.");
        }

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            normalizedClaimedBy,
            leaseDuration,
            maxCount,
            cancellationToken);

        var dispatchedCount = 0;
        var retryScheduledCount = 0;
        var deadLetteredCount = 0;
        var staleCount = 0;

        foreach (DurableOutboxRecord record in records)
        {
            bool renewed = await leaseRenewer.RenewAsync(
                record.Envelope.MessageId,
                normalizedClaimedBy,
                leaseDuration,
                cancellationToken);

            if (!renewed)
            {
                staleCount++;
                continue;
            }

            try
            {
                await transport.SendAsync(record, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                DurableOutboxFailureDecision decision = failurePolicy.DecideFailure(
                    record,
                    CreateFailureReason(exception),
                    _timeProvider.GetUtcNow());

                bool recorded = await RecordFailureAsync(
                    record,
                    normalizedClaimedBy,
                    decision,
                    cancellationToken);

                if (!recorded)
                {
                    staleCount++;
                    continue;
                }

                if (decision.ShouldRetry)
                {
                    retryScheduledCount++;
                    continue;
                }

                deadLetteredCount++;
                continue;
            }

            bool dispatched = await dispatchRecorder.MarkDispatchedAsync(
                record.Envelope.MessageId,
                normalizedClaimedBy,
                _timeProvider.GetUtcNow(),
                cancellationToken);

            if (dispatched)
            {
                dispatchedCount++;
            }
            else
            {
                staleCount++;
            }
        }

        return new DurableOutboxDispatchResult(
            records.Count,
            dispatchedCount,
            retryScheduledCount,
            deadLetteredCount,
            staleCount);
    }

    private async ValueTask<bool> RecordFailureAsync(
        DurableOutboxRecord record,
        string claimedBy,
        DurableOutboxFailureDecision decision,
        CancellationToken cancellationToken)
    {
        if (decision.ShouldRetry)
        {
            return await dispatchRecorder.ScheduleRetryAsync(
                record.Envelope.MessageId,
                claimedBy,
                decision.FailureReason,
                decision.FailedAtUtc,
                decision.NextAttemptAtUtc!.Value,
                cancellationToken);
        }

        return await dispatchRecorder.MarkDeadLetteredAsync(
            record.Envelope.MessageId,
            claimedBy,
            decision.FailureReason,
            decision.FailedAtUtc,
            cancellationToken);
    }

    private static string CreateFailureReason(Exception exception)
    {
        string exceptionType = exception.GetType().FullName ?? exception.GetType().Name;

        if (string.IsNullOrWhiteSpace(exception.Message))
        {
            return exceptionType;
        }

        return $"{exceptionType}: {exception.Message}";
    }
}
