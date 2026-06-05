using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableInboxHandleResultTests
{
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(DurableInboxHandleStatus.Handled, true, false)]
    [InlineData(DurableInboxHandleStatus.AlreadyReceived, false, true)]
    [InlineData(DurableInboxHandleStatus.AlreadyProcessed, false, true)]
    public void Constructor_WhenStatusIsSupported_SetsFlags(
        DurableInboxHandleStatus status,
        bool wasHandled,
        bool wasSkipped)
    {
        DurableInboxRecord record = CreateRecord();

        var result = new DurableInboxHandleResult(status, record);

        Assert.Equal(status, result.Status);
        Assert.Same(record, result.Record);
        Assert.Equal(wasHandled, result.WasHandled);
        Assert.Equal(wasSkipped, result.WasSkipped);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStatusIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DurableInboxHandleResult(
                (DurableInboxHandleStatus)999,
                CreateRecord()));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenRecordIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DurableInboxHandleResult(
                DurableInboxHandleStatus.Handled,
                null!));
    }

    private static DurableInboxRecord CreateRecord()
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(
                Guid.Parse("77b326f5-4186-46d2-bb46-40eefc0d8d45"),
                "sales",
                "sales.customer.registered.v1"),
            new DateTimeOffset(2026, 6, 5, 8, 0, 0, TimeSpan.Zero));
    }
}
