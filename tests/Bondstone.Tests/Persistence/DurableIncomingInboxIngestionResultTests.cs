using Bondstone.Messaging;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableIncomingInboxIngestionResultTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenIngested_CarriesRecord()
    {
        DurableIncomingInboxRecord record = CreateRecord();

        var result = new DurableIncomingInboxIngestionResult(
            DurableIncomingInboxIngestionStatus.Ingested,
            record);

        Assert.Equal(DurableIncomingInboxIngestionStatus.Ingested, result.Status);
        Assert.Same(record, result.Record);
        Assert.True(result.WasIngested);
        Assert.False(result.WasAlreadyIngested);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenAlreadyIngested_CarriesDuplicateRecord()
    {
        DurableIncomingInboxRecord record = CreateRecord();

        var result = new DurableIncomingInboxIngestionResult(
            DurableIncomingInboxIngestionStatus.AlreadyIngested,
            record);

        Assert.Equal(DurableIncomingInboxIngestionStatus.AlreadyIngested, result.Status);
        Assert.False(result.WasIngested);
        Assert.True(result.WasAlreadyIngested);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStatusIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DurableIncomingInboxIngestionResult(
                (DurableIncomingInboxIngestionStatus)999,
                CreateRecord()));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenRecordIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DurableIncomingInboxIngestionResult(
                DurableIncomingInboxIngestionStatus.Ingested,
                record: null!));
    }

    private static DurableIncomingInboxRecord CreateRecord()
    {
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "ordering.reserve-inventory.v1",
            "ordering",
            "fulfillment",
            "{}",
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"));

        return new DurableIncomingInboxRecord(
            DurableIncomingInboxKey.ForCommandHandler(
                envelope.MessageId,
                "fulfillment",
                "fulfillment.reserve-inventory.v1"),
            envelope,
            DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"));
    }
}
