using Bondstone.Messaging;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableIncomingInboxRecordTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenCommandValuesAreValid_CarriesEnvelopeAndIncomingInboxState()
    {
        DurableMessageEnvelope envelope = CreateCommandEnvelope();
        DurableIncomingInboxKey key = DurableIncomingInboxKey.ForCommandHandler(
            envelope.MessageId,
            "fulfillment",
            "fulfillment.reserve-inventory.v1");
        DateTimeOffset ingestedAtUtc = DateTimeOffset.Parse("2026-06-17T00:01:00+00:00");

        var record = new DurableIncomingInboxRecord(
            key,
            envelope,
            ingestedAtUtc,
            sourceTransportName: " rabbitmq:orders ");

        Assert.Same(key, record.Key);
        Assert.Same(envelope, record.Envelope);
        Assert.Equal("fulfillment", record.ReceiverModule);
        Assert.Equal("fulfillment.reserve-inventory.v1", record.HandlerIdentity);
        Assert.Equal("rabbitmq:orders", record.SourceTransportName);
        Assert.Equal(ingestedAtUtc, record.IngestedAtUtc);
        Assert.Same(DurableIncomingInboxState.Pending, record.State);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenEventValuesAreValid_AllowsSubscriberReceiveIdentity()
    {
        DurableMessageEnvelope envelope = CreateEventEnvelope();
        DurableIncomingInboxKey key = DurableIncomingInboxKey.ForEventSubscriber(
            envelope.MessageId,
            "billing",
            "billing.order-projection.v1");
        DurableIncomingInboxState state = new(
            DurableIncomingInboxStatus.Processing,
            attemptCount: 1,
            claimedBy: "worker-1",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00"));

        var record = new DurableIncomingInboxRecord(
            key,
            envelope,
            DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
            state);

        Assert.Equal("billing", record.ReceiverModule);
        Assert.Equal("billing.order-projection.v1", record.HandlerIdentity);
        Assert.Same(state, record.State);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenKeyIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DurableIncomingInboxRecord(
                key: null!,
                CreateCommandEnvelope(),
                DateTimeOffset.Parse("2026-06-17T00:01:00+00:00")));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenEnvelopeIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DurableIncomingInboxRecord(
                CreateCommandKey(),
                envelope: null!,
                DateTimeOffset.Parse("2026-06-17T00:01:00+00:00")));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenIngestedAtUtcIsDefault_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateRecord(ingestedAtUtc: DateTimeOffset.MinValue));

        Assert.Equal("ingestedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenIngestedAtUtcHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateRecord(ingestedAtUtc: DateTimeOffset.Parse("2026-06-17T00:01:00+02:00")));

        Assert.Equal("ingestedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenKeyMessageIdDiffersFromEnvelope_Throws()
    {
        DurableIncomingInboxKey key = DurableIncomingInboxKey.ForCommandHandler(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "fulfillment",
            "fulfillment.reserve-inventory.v1");

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateRecord(key: key));

        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenCommandReceiverDiffersFromEnvelopeTarget_Throws()
    {
        DurableMessageEnvelope envelope = CreateCommandEnvelope();
        DurableIncomingInboxKey key = DurableIncomingInboxKey.ForCommandHandler(
            envelope.MessageId,
            "shipping",
            "shipping.reserve-inventory.v1");

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateRecord(key: key, envelope: envelope));

        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStateTimestampIsEarlierThanIngested_Throws()
    {
        DurableIncomingInboxState state = new(
            DurableIncomingInboxStatus.Processed,
            attemptCount: 1,
            processedAtUtc: DateTimeOffset.Parse("2026-06-17T00:00:59+00:00"));

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateRecord(
                ingestedAtUtc: DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
                state: state));

        Assert.Equal("state", exception.ParamName);
    }

    private static DurableIncomingInboxRecord CreateRecord(
        DurableIncomingInboxKey? key = null,
        DurableMessageEnvelope? envelope = null,
        DateTimeOffset? ingestedAtUtc = null,
        DurableIncomingInboxState? state = null)
    {
        DurableMessageEnvelope effectiveEnvelope = envelope ?? CreateCommandEnvelope();

        return new DurableIncomingInboxRecord(
            key ?? DurableIncomingInboxKey.ForCommandHandler(
                effectiveEnvelope.MessageId,
                "fulfillment",
                "fulfillment.reserve-inventory.v1"),
            effectiveEnvelope,
            ingestedAtUtc ?? DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
            state);
    }

    private static DurableIncomingInboxKey CreateCommandKey()
    {
        return DurableIncomingInboxKey.ForCommandHandler(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "fulfillment",
            "fulfillment.reserve-inventory.v1");
    }

    private static DurableMessageEnvelope CreateCommandEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "ordering.reserve-inventory.v1",
            "ordering",
            "fulfillment",
            "{}",
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"));
    }

    private static DurableMessageEnvelope CreateEventEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            MessageKind.Event,
            "ordering.order-submitted.v1",
            "ordering",
            targetModule: null,
            "{}",
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"));
    }
}
