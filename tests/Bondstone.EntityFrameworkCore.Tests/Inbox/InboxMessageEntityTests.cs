using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Tests.Inbox;

public sealed class InboxMessageEntityTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void FromRecord_WhenRecordIsValid_MapsInboxFields()
    {
        var record = new DurableInboxRecord(
            new DurableInboxMessageKey(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "sales",
                "sales.customer.registered.v1"),
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"),
            DateTimeOffset.Parse("2026-06-04T00:00:01+00:00"));

        InboxMessageEntity entity = InboxMessageEntity.FromRecord(record);

        Assert.Equal(record.Key.MessageId, entity.MessageId);
        Assert.Equal(record.Key.ModuleName, entity.ModuleName);
        Assert.Equal(record.Key.HandlerIdentity, entity.HandlerIdentity);
        Assert.Equal(record.ReceivedAtUtc, entity.ReceivedAtUtc);
        Assert.Equal(record.ProcessedAtUtc, entity.ProcessedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ToRecord_WhenEntityWasMapped_RoundTripsInboxRecord()
    {
        var record = new DurableInboxRecord(
            new DurableInboxMessageKey(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "sales",
                "sales.customer.registered.v1"),
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
        InboxMessageEntity entity = InboxMessageEntity.FromRecord(record);

        DurableInboxRecord mapped = entity.ToRecord();

        Assert.Equal(record, mapped);
    }
}
