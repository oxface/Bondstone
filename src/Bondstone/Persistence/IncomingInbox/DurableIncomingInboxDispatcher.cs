using System.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class DurableIncomingInboxDispatcher(
    IDurableIncomingInboxClaimer claimer,
    IModuleCommandReceivePipeline commandReceivePipeline,
    IModuleEventReceivePipeline eventReceivePipeline,
    IDurableIncomingInboxOutcomeRecorder outcomeRecorder,
    IDurableIncomingInboxFailurePolicy failurePolicy,
    TimeProvider? timeProvider = null)
    : IDurableIncomingInboxDispatcher
{
    private readonly IDurableIncomingInboxClaimer _claimer =
        claimer ?? throw new ArgumentNullException(nameof(claimer));
    private readonly IModuleCommandReceivePipeline _commandReceivePipeline =
        commandReceivePipeline ?? throw new ArgumentNullException(nameof(commandReceivePipeline));
    private readonly IModuleEventReceivePipeline _eventReceivePipeline =
        eventReceivePipeline ?? throw new ArgumentNullException(nameof(eventReceivePipeline));
    private readonly IDurableIncomingInboxOutcomeRecorder _outcomeRecorder =
        outcomeRecorder ?? throw new ArgumentNullException(nameof(outcomeRecorder));
    private readonly IDurableIncomingInboxFailurePolicy _failurePolicy =
        failurePolicy ?? throw new ArgumentNullException(nameof(failurePolicy));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async ValueTask<DurableIncomingInboxProcessingResult> ProcessAsync(
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
                "Maximum processing count must be positive.");
        }

        using Activity? activity = IncomingInboxProcessingDiagnostics.ActivitySource.StartActivity(
            IncomingInboxProcessingDiagnostics.ProcessActivityName,
            ActivityKind.Internal);
        activity?.SetTag(IncomingInboxProcessingDiagnostics.Tags.MaxCount, maxCount);

        try
        {
            IReadOnlyList<DurableIncomingInboxRecord> records = await _claimer.ClaimAsync(
                normalizedClaimedBy,
                leaseDuration,
                maxCount,
                ct);
            IncomingInboxProcessingDiagnostics.RecordClaimed(records);

            var processedCount = 0;
            var retryScheduledCount = 0;
            var terminalFailedCount = 0;
            var staleCount = 0;

            foreach (DurableIncomingInboxRecord record in records)
            {
                using Activity? messageActivity =
                    IncomingInboxProcessingDiagnostics.ActivitySource.StartActivity(
                        IncomingInboxProcessingDiagnostics.ProcessMessageActivityName,
                        ActivityKind.Internal);
                if (messageActivity is not null)
                {
                    IncomingInboxProcessingDiagnostics.SetRecordTags(messageActivity, record);
                }

                try
                {
                    await ReceiveAsync(record, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    messageActivity?.SetStatus(ActivityStatusCode.Error, exception.Message);
                    DurableIncomingInboxFailureDecision decision = _failurePolicy.DecideFailure(
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
                        IncomingInboxProcessingDiagnostics.RecordStale(record);
                        continue;
                    }

                    if (decision.ShouldRetry)
                    {
                        retryScheduledCount++;
                        IncomingInboxProcessingDiagnostics.RecordRetryScheduled(record);
                        continue;
                    }

                    terminalFailedCount++;
                    IncomingInboxProcessingDiagnostics.RecordTerminalFailed(record);
                    continue;
                }

                bool processed = await _outcomeRecorder.MarkProcessedAsync(
                    record.Key,
                    normalizedClaimedBy,
                    _timeProvider.GetUtcNow(),
                    ct);

                if (processed)
                {
                    processedCount++;
                    IncomingInboxProcessingDiagnostics.RecordProcessed(record);
                }
                else
                {
                    staleCount++;
                    IncomingInboxProcessingDiagnostics.RecordStale(record);
                }
            }

            var result = new DurableIncomingInboxProcessingResult(
                records.Count,
                processedCount,
                retryScheduledCount,
                terminalFailedCount,
                staleCount);
            if (activity is not null)
            {
                IncomingInboxProcessingDiagnostics.SetProcessingResultTags(activity, result);
            }

            return result;
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            throw;
        }
    }

    private async ValueTask ReceiveAsync(
        DurableIncomingInboxRecord record,
        CancellationToken ct)
    {
        // Long-running handler lease renewal needs a separate heartbeat loop tied
        // to handler lifetime. A one-time renewal before receive would not protect
        // the claim while the module pipeline is executing.
        DurableMessageEnvelope envelope = record.Envelope;
        switch (envelope.MessageKind)
        {
            case MessageKind.Command:
                await _commandReceivePipeline.HandleOnceAsync(envelope, ct);
                return;
            case MessageKind.Event:
                await _eventReceivePipeline.HandleOnceAsync(
                    envelope,
                    record.ReceiverModule,
                    record.HandlerIdentity,
                    ct);
                return;
            default:
                throw new NotSupportedException(
                    $"Incoming inbox processing does not support message kind '{envelope.MessageKind}'.");
        }
    }

    private async ValueTask<bool> RecordFailureAsync(
        DurableIncomingInboxRecord record,
        string claimedBy,
        DurableIncomingInboxFailureDecision decision,
        CancellationToken ct)
    {
        if (decision.ShouldRetry)
        {
            return await _outcomeRecorder.ScheduleRetryAsync(
                record.Key,
                claimedBy,
                decision.FailureReason,
                decision.FailedAtUtc,
                decision.NextAttemptAtUtc!.Value,
                ct);
        }

        return await _outcomeRecorder.MarkTerminalFailedAsync(
            record.Key,
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
