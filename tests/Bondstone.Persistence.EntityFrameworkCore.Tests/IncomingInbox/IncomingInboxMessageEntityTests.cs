using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.IncomingInbox;

public sealed class IncomingInboxMessageEntityTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void FromRecord_WhenRecordIsValid_MapsIncomingInboxFields()
    {
        DurableIncomingInboxRecord record = CreateRecord();

        IncomingInboxMessageEntity entity = IncomingInboxMessageEntity.FromRecord(record);

        Assert.Equal(record.Envelope.MessageId, entity.MessageId);
        Assert.Equal(record.Envelope.MessageKind, entity.MessageKind);
        Assert.Equal(record.Envelope.MessageTypeName, entity.MessageTypeName);
        Assert.Equal(record.Envelope.SourceModule, entity.SourceModule);
        Assert.Equal(record.Envelope.TargetModule, entity.TargetModule);
        Assert.Equal(record.Envelope.DurableOperationId, entity.DurableOperationId);
        Assert.Equal(record.Envelope.TraceContext?.TraceParent, entity.TraceParent);
        Assert.Equal(record.Envelope.TraceContext?.TraceState, entity.TraceState);
        Assert.Equal(record.Envelope.TraceContext?.Baggage, entity.TraceBaggage);
        Assert.Equal(record.Envelope.CausationId, entity.CausationId);
        Assert.Equal(record.Envelope.PartitionKey, entity.PartitionKey);
        Assert.Equal(record.Envelope.Payload, entity.Payload);
        Assert.Equal(record.Envelope.Metadata, entity.Metadata);
        Assert.Equal(record.Envelope.CreatedAtUtc, entity.CreatedAtUtc);
        Assert.Equal(record.ReceiverModule, entity.ReceiverModule);
        Assert.Equal(record.HandlerIdentity, entity.HandlerIdentity);
        Assert.Equal(record.SourceTransportName, entity.SourceTransportName);
        Assert.Equal(record.IngestedAtUtc, entity.IngestedAtUtc);
        Assert.Equal(record.State.Status, entity.Status);
        Assert.Equal(record.State.AttemptCount, entity.AttemptCount);
        Assert.Equal(record.State.NextAttemptAtUtc, entity.NextAttemptAtUtc);
        Assert.Equal(record.State.ProcessedAtUtc, entity.ProcessedAtUtc);
        Assert.Equal(record.State.FailedAtUtc, entity.FailedAtUtc);
        Assert.Equal(record.State.FailureReason, entity.FailureReason);
        Assert.Equal(record.State.ClaimedBy, entity.ClaimedBy);
        Assert.Equal(record.State.ClaimedUntilUtc, entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ToRecord_WhenEntityWasMapped_RoundTripsIncomingInboxRecord()
    {
        DurableIncomingInboxRecord record = CreateRecord();
        IncomingInboxMessageEntity entity = IncomingInboxMessageEntity.FromRecord(record);

        DurableIncomingInboxRecord mapped = entity.ToRecord();

        Assert.Equal(record, mapped);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ToRecord_WhenEventEntityWasMapped_RoundTripsSubscriberRecord()
    {
        DurableIncomingInboxRecord record = CreateEventRecord();
        IncomingInboxMessageEntity entity = IncomingInboxMessageEntity.FromRecord(record);

        DurableIncomingInboxRecord mapped = entity.ToRecord();

        Assert.Equal(record, mapped);
    }

    private static DurableIncomingInboxRecord CreateRecord()
    {
        var traceContext = new MessageTraceContext(
            "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
            "congo=t61rcWkgMzE",
            "tenant=ordering");
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "ordering.reserve-inventory.v1",
            "ordering",
            "fulfillment",
            """ {"orderId":"order-123"} """,
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            traceContext,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "order-123",
            """ {"metadata":true} """);
        var key = DurableIncomingInboxKey.ForCommandHandler(
            envelope.MessageId,
            "fulfillment",
            "fulfillment.reserve-inventory.v1");
        var state = new DurableIncomingInboxState(
            DurableIncomingInboxStatus.RetryScheduled,
            attemptCount: 2,
            nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"),
            failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:01:30+00:00"),
            failureReason: "failed once");

        return new DurableIncomingInboxRecord(
            key,
            envelope,
            DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
            state,
            "rabbitmq:orders");
    }

    private static DurableIncomingInboxRecord CreateEventRecord()
    {
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            MessageKind.Event,
            "ordering.order-submitted.v1",
            "ordering",
            targetModule: null,
            """ {"orderId":"order-123"} """,
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"));
        var key = DurableIncomingInboxKey.ForEventSubscriber(
            envelope.MessageId,
            "billing",
            "billing.order-projection.v1");
        var state = new DurableIncomingInboxState(
            DurableIncomingInboxStatus.Processing,
            attemptCount: 1,
            claimedBy: "worker-1",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00"));

        return new DurableIncomingInboxRecord(
            key,
            envelope,
            DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
            state);
    }
}
