using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableOutboxDispatcher(
    IDurableOutboxClaimer claimer,
    IDurableOutboxLeaseRenewer leaseRenewer,
    IDurableEnvelopeDispatcher envelopeDispatcher,
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
        CancellationToken ct = default)
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
            ct);

        var dispatchedCount = 0;
        var retryScheduledCount = 0;
        var terminalFailedCount = 0;
        var staleCount = 0;

        foreach (DurableOutboxRecord record in records)
        {
            bool renewed = await leaseRenewer.RenewAsync(
                record.Envelope.MessageId,
                normalizedClaimedBy,
                leaseDuration,
                ct);

            if (!renewed)
            {
                staleCount++;
                continue;
            }

            try
            {
                await envelopeDispatcher.DispatchAsync(record, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
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
                    ct);

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

                terminalFailedCount++;
                continue;
            }

            bool dispatched = await dispatchRecorder.MarkDispatchedAsync(
                record.Envelope.MessageId,
                normalizedClaimedBy,
                _timeProvider.GetUtcNow(),
                ct);

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
            terminalFailedCount,
            staleCount);
    }

    private async ValueTask<bool> RecordFailureAsync(
        DurableOutboxRecord record,
        string claimedBy,
        DurableOutboxFailureDecision decision,
        CancellationToken ct)
    {
        if (decision.ShouldRetry)
        {
            return await dispatchRecorder.ScheduleRetryAsync(
                record.Envelope.MessageId,
                claimedBy,
                decision.FailureReason,
                decision.FailedAtUtc,
                decision.NextAttemptAtUtc!.Value,
                ct);
        }

        return await dispatchRecorder.MarkTerminalFailedAsync(
            record.Envelope.MessageId,
            claimedBy,
            decision.FailureReason,
            decision.FailedAtUtc,
            ct);
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
