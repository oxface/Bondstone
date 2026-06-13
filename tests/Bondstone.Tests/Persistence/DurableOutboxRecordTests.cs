using Bondstone.Messaging;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableOutboxRecordTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_CarriesEnvelopeAndStoredTimestamp()
    {
        DurableMessageEnvelope envelope = CreateEnvelope();
        DateTimeOffset storedAtUtc = DateTimeOffset.Parse("2026-06-04T00:00:01+00:00");

        var record = new DurableOutboxRecord(envelope, storedAtUtc);

        Assert.Same(envelope, record.Envelope);
        Assert.Equal(storedAtUtc, record.StoredAtUtc);
        Assert.Same(DurableOutboxDispatchState.Pending, record.DispatchState);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenDispatchStateIsSpecified_CarriesDispatchState()
    {
        DurableOutboxDispatchState dispatchState = new(
            DurableOutboxStatus.Dispatched,
            attemptCount: 1,
            dispatchedAtUtc: DateTimeOffset.Parse("2026-06-04T00:00:02+00:00"));

        var record = new DurableOutboxRecord(
            CreateEnvelope(),
            DateTimeOffset.Parse("2026-06-04T00:00:01+00:00"),
            dispatchState);

        Assert.Same(dispatchState, record.DispatchState);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenEnvelopeIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new DurableOutboxRecord(
                envelope: null!,
                DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStoredAtUtcIsDefault_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new DurableOutboxRecord(CreateEnvelope(), DateTimeOffset.MinValue));

        Assert.Equal("storedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStoredAtUtcHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new DurableOutboxRecord(
                CreateEnvelope(),
                DateTimeOffset.Parse("2026-06-04T00:00:01+02:00")));

        Assert.Equal("storedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenDispatchTimestampIsEarlierThanStoredAtUtc_Throws()
    {
        DurableOutboxDispatchState dispatchState = new(
            DurableOutboxStatus.Failed,
            attemptCount: 1,
            failedAtUtc: DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new DurableOutboxRecord(
                CreateEnvelope(),
                DateTimeOffset.Parse("2026-06-04T00:00:01+00:00"),
                dispatchState));

        Assert.Equal("dispatchState", exception.ParamName);
    }

    private static DurableMessageEnvelope CreateEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "sales.customer.register.v1",
            "sales",
            "billing",
            "{}",
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
    }
}
