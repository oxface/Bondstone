using System.Diagnostics;
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

        using Activity? activity = BondstonePersistenceDiagnostics.ActivitySource.StartActivity(
            BondstonePersistenceDiagnostics.OutboxDispatchActivityName,
            ActivityKind.Internal);
        activity?.SetTag(BondstonePersistenceDiagnostics.Tags.ClaimedBy, normalizedClaimedBy);
        activity?.SetTag(BondstonePersistenceDiagnostics.Tags.MaxCount, maxCount);

        try
        {
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

                using Activity? dispatchActivity =
                    BondstonePersistenceDiagnostics.ActivitySource.StartActivity(
                        BondstonePersistenceDiagnostics.OutboxDispatchActivityName + ".message",
                        ActivityKind.Internal);
                if (dispatchActivity is not null)
                {
                    BondstonePersistenceDiagnostics.SetRecordTags(dispatchActivity, record);
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
                    dispatchActivity?.SetStatus(ActivityStatusCode.Error, exception.Message);
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

            var result = new DurableOutboxDispatchResult(
                records.Count,
                dispatchedCount,
                retryScheduledCount,
                terminalFailedCount,
                staleCount);
            if (activity is not null)
            {
                BondstonePersistenceDiagnostics.SetDispatchResultTags(activity, result);
            }

            return result;
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
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
