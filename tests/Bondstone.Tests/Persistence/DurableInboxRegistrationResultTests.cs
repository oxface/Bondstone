using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableInboxRegistrationResultTests
{
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(DurableInboxRegistrationStatus.Registered, true, false)]
    [InlineData(DurableInboxRegistrationStatus.AlreadyReceived, false, true)]
    [InlineData(DurableInboxRegistrationStatus.AlreadyProcessed, false, true)]
    public void Constructor_WhenStatusIsValid_CarriesRegistrationResult(
        DurableInboxRegistrationStatus status,
        bool isRegistered,
        bool isDuplicate)
    {
        DurableInboxRecord record = CreateRecord();

        var result = new DurableInboxRegistrationResult(status, record);

        Assert.Equal(status, result.Status);
        Assert.Same(record, result.Record);
        Assert.Equal(isRegistered, result.IsRegistered);
        Assert.Equal(isDuplicate, result.IsDuplicate);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStatusIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DurableInboxRegistrationResult(
                (DurableInboxRegistrationStatus)999,
                CreateRecord()));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenRecordIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DurableInboxRegistrationResult(
                DurableInboxRegistrationStatus.Registered,
                record: null!));
    }

    private static DurableInboxRecord CreateRecord()
    {
        return new DurableInboxRecord(
            new DurableInboxMessageKey(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "fulfillment",
                "fulfillment.submit-order.v1"),
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
    }
}
