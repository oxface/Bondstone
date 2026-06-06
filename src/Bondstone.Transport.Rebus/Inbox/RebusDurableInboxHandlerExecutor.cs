using System.Diagnostics;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;

namespace Bondstone.Transport.Rebus.Inbox;

public sealed class RebusDurableInboxHandlerExecutor(
    IDurableInboxHandlerExecutor inboxHandlerExecutor,
    TimeProvider? timeProvider = null)
    : IRebusDurableInboxHandlerExecutor
{
    private readonly IDurableInboxHandlerExecutor _inboxHandlerExecutor =
        inboxHandlerExecutor ?? throw new ArgumentNullException(nameof(inboxHandlerExecutor));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
        RebusDurableMessageEnvelope envelope,
        string handlerIdentity,
        Func<DurableMessageEnvelope, CancellationToken, ValueTask> handler,
        Func<CancellationToken, ValueTask> commit,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(commit);

        DurableMessageEnvelope durableEnvelope = ToDurableEnvelope(envelope);
        var record = new DurableInboxRecord(
            new DurableInboxMessageKey(
                durableEnvelope.MessageId,
                durableEnvelope.TargetModule!,
                handlerIdentity),
            _timeProvider.GetUtcNow());

        DurableInboxHandleResult result = await _inboxHandlerExecutor.HandleOnceAsync(
            record,
            handlerCt => handler(durableEnvelope, handlerCt),
            commit,
            ct);

        if (result.Status == DurableInboxHandleStatus.AlreadyReceived)
        {
            throw new RebusDurableInboxAlreadyReceivedException(result);
        }

        return result;
    }

    private static DurableMessageEnvelope ToDurableEnvelope(
        RebusDurableMessageEnvelope envelope)
    {
        if (!Enum.TryParse(envelope.MessageKind, out MessageKind messageKind)
            || !Enum.IsDefined(messageKind))
        {
            throw new NotSupportedException(
                $"Rebus durable inbox message kind '{envelope.MessageKind}' is not supported.");
        }

        if (messageKind != MessageKind.Command)
        {
            throw new NotSupportedException(
                "The first Rebus inbox integration supports command envelopes only.");
        }

        return new DurableMessageEnvelope(
            envelope.MessageId,
            messageKind,
            envelope.MessageTypeName,
            envelope.SourceModule,
            envelope.TargetModule,
            envelope.Payload,
            envelope.CreatedAtUtc,
            durableOperationId: envelope.DurableOperationId,
            traceContext: CreateTraceContext(envelope),
            causationId: envelope.CausationId,
            partitionKey: envelope.PartitionKey,
            metadata: envelope.Metadata);
    }

    private static MessageTraceContext? CreateTraceContext(
        RebusDurableMessageEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.TraceParent))
        {
            return null;
        }

        if (!ActivityContext.TryParse(envelope.TraceParent, envelope.TraceState, out _))
        {
            throw new ArgumentException(
                "Trace parent must be a valid W3C traceparent value.",
                nameof(envelope));
        }

        return new MessageTraceContext(
            envelope.TraceParent,
            envelope.TraceState,
            envelope.TraceBaggage);
    }
}
