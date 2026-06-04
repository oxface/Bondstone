using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.Messaging;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Tests.Operations;

public sealed class OperationStateEntityTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void FromState_WhenStateIsValid_MapsOperationFields()
    {
        var state = new DurableOperationState(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DurableOperationStatus.Completed,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"),
            """ {"ok":true} """,
            " failed once ");

        OperationStateEntity entity = OperationStateEntity.FromState(state);

        Assert.Equal(state.DurableOperationId, entity.DurableOperationId);
        Assert.Equal(state.Status, entity.Status);
        Assert.Equal(state.UpdatedAtUtc, entity.UpdatedAtUtc);
        Assert.Equal(state.ResultPayload, entity.ResultPayload);
        Assert.Equal(state.FailureReason, entity.FailureReason);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ToState_WhenEntityWasMapped_RoundTripsOperationState()
    {
        var state = new DurableOperationState(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DurableOperationStatus.Running,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
        OperationStateEntity entity = OperationStateEntity.FromState(state);

        DurableOperationState mapped = entity.ToState();

        Assert.Equal(state, mapped);
    }
}
