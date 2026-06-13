using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableOperationStateTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_CarriesOperationState()
    {
        Guid durableOperationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        DateTimeOffset updatedAtUtc = DateTimeOffset.Parse("2026-06-04T00:00:00+00:00");

        var state = new DurableOperationState(
            durableOperationId,
            DurableOperationStatus.Completed,
            updatedAtUtc,
            """ {"result":true} """,
            " failed once ");

        Assert.Equal(durableOperationId, state.DurableOperationId);
        Assert.Equal(DurableOperationStatus.Completed, state.Status);
        Assert.Equal(updatedAtUtc, state.UpdatedAtUtc);
        Assert.Equal(""" {"result":true} """, state.ResultPayload);
        Assert.Equal(" failed once ", state.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenOptionalPayloadsAreWhitespace_StoresNull()
    {
        var state = new DurableOperationState(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DurableOperationStatus.Pending,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"),
            resultPayload: " ",
            failureReason: " ");

        Assert.Null(state.ResultPayload);
        Assert.Null(state.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenDurableOperationIdIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(durableOperationId: Guid.Empty));

        Assert.Equal("durableOperationId", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStatusIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateState(status: (DurableOperationStatus)999));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenUpdatedAtUtcIsDefault_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(updatedAtUtc: DateTimeOffset.MinValue));

        Assert.Equal("updatedAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenUpdatedAtUtcHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateState(updatedAtUtc: DateTimeOffset.Parse("2026-06-04T00:00:00+02:00")));

        Assert.Equal("updatedAtUtc", exception.ParamName);
    }

    private static DurableOperationState CreateState(
        Guid? durableOperationId = null,
        DurableOperationStatus status = DurableOperationStatus.Pending,
        DateTimeOffset? updatedAtUtc = null)
    {
        return new DurableOperationState(
            durableOperationId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            status,
            updatedAtUtc ?? DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
    }
}
