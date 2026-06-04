using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableInboxRecordTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_CarriesInboxState()
    {
        DurableInboxMessageKey key = CreateKey();
        DateTimeOffset receivedAtUtc = DateTimeOffset.Parse("2026-06-04T00:00:00+00:00");

        var record = new DurableInboxRecord(key, receivedAtUtc);

        Assert.Same(key, record.Key);
        Assert.Equal(receivedAtUtc, record.ReceivedAtUtc);
        Assert.Null(record.ProcessedAtUtc);
        Assert.False(record.IsProcessed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MarkProcessed_WhenTimestampIsValid_ReturnsProcessedRecord()
    {
        DurableInboxMessageKey key = CreateKey();
        DateTimeOffset receivedAtUtc = DateTimeOffset.Parse("2026-06-04T00:00:00+00:00");
        DateTimeOffset processedAtUtc = DateTimeOffset.Parse("2026-06-04T00:01:00+00:00");
        var record = new DurableInboxRecord(key, receivedAtUtc);

        DurableInboxRecord processed = record.MarkProcessed(processedAtUtc);

        Assert.NotSame(record, processed);
        Assert.Same(key, processed.Key);
        Assert.Equal(receivedAtUtc, processed.ReceivedAtUtc);
        Assert.Equal(processedAtUtc, processed.ProcessedAtUtc);
        Assert.True(processed.IsProcessed);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenKeyIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DurableInboxRecord(
                key: null!,
                DateTimeOffset.Parse("2026-06-04T00:00:00+00:00")));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenReceivedAtUtcIsDefault_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateRecord(receivedAtUtc: DateTimeOffset.MinValue));

        Assert.Equal("receivedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenReceivedAtUtcHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateRecord(receivedAtUtc: DateTimeOffset.Parse("2026-06-04T00:00:00+02:00")));

        Assert.Equal("receivedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenProcessedAtUtcHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateRecord(processedAtUtc: DateTimeOffset.Parse("2026-06-04T00:01:00+02:00")));

        Assert.Equal("processedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenProcessedAtUtcIsEarlierThanReceivedAtUtc_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateRecord(
                receivedAtUtc: DateTimeOffset.Parse("2026-06-04T00:01:00+00:00"),
                processedAtUtc: DateTimeOffset.Parse("2026-06-04T00:00:00+00:00")));

        Assert.Equal("processedAtUtc", exception.ParamName);
    }

    private static DurableInboxRecord CreateRecord(
        DateTimeOffset? receivedAtUtc = null,
        DateTimeOffset? processedAtUtc = null)
    {
        return new DurableInboxRecord(
            CreateKey(),
            receivedAtUtc ?? DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"),
            processedAtUtc);
    }

    private static DurableInboxMessageKey CreateKey()
    {
        return new DurableInboxMessageKey(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "sales",
            "sales.customer.registered.v1");
    }
}
