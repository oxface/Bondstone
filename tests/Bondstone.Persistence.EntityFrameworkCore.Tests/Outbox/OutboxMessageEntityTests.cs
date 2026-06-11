using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.Outbox;

public sealed class OutboxMessageEntityTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void FromRecord_WhenRecordIsValid_MapsOutboxFields()
    {
        DurableOutboxRecord record = CreateRecord();

        OutboxMessageEntity entity = OutboxMessageEntity.FromRecord(record);

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
        Assert.Equal(record.StoredAtUtc, entity.StoredAtUtc);
        Assert.Equal(record.DispatchState.Status, entity.Status);
        Assert.Equal(record.DispatchState.AttemptCount, entity.AttemptCount);
        Assert.Equal(record.DispatchState.NextAttemptAtUtc, entity.NextAttemptAtUtc);
        Assert.Equal(record.DispatchState.DispatchedAtUtc, entity.DispatchedAtUtc);
        Assert.Equal(record.DispatchState.FailedAtUtc, entity.FailedAtUtc);
        Assert.Equal(record.DispatchState.FailureReason, entity.FailureReason);
        Assert.Equal(record.DispatchState.ClaimedBy, entity.ClaimedBy);
        Assert.Equal(record.DispatchState.ClaimedUntilUtc, entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ToRecord_WhenEntityWasMapped_RoundTripsOutboxRecord()
    {
        DurableOutboxRecord record = CreateRecord();
        OutboxMessageEntity entity = OutboxMessageEntity.FromRecord(record);

        DurableOutboxRecord mapped = entity.ToRecord();

        Assert.Equal(record, mapped);
    }

    private static DurableOutboxRecord CreateRecord()
    {
        var traceContext = new MessageTraceContext(
            "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
            "congo=t61rcWkgMzE",
            "tenant=sales");
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "sales.customer.register.v1",
            "sales",
            "billing",
            """ {"customerId":"customer-123"} """,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            traceContext,
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "customer-123",
            """ {"metadata":true} """);
        var dispatchState = new DurableOutboxDispatchState(
            DurableOutboxStatus.Failed,
            attemptCount: 2,
            nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-04T00:01:00+00:00"),
            failedAtUtc: DateTimeOffset.Parse("2026-06-04T00:00:30+00:00"),
            failureReason: "failed once",
            claimedBy: "dispatcher-1",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-04T00:02:00+00:00"));

        return new DurableOutboxRecord(
            envelope,
            DateTimeOffset.Parse("2026-06-04T00:00:01+00:00"),
            dispatchState);
    }
}
