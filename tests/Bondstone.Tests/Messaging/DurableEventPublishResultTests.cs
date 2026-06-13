using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableEventPublishResultTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_CarriesPublishState()
    {
        Guid publishId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid durableOperationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var result = new DurableEventPublishResult(
            publishId,
            durableOperationId,
            DurableEventPublishStatus.Accepted);

        Assert.Equal(publishId, result.PublishId);
        Assert.Equal(durableOperationId, result.DurableOperationId);
        Assert.Equal(DurableEventPublishStatus.Accepted, result.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenPublishIdIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new DurableEventPublishResult(
                Guid.Empty,
                durableOperationId: null,
                DurableEventPublishStatus.Accepted));

        Assert.Equal("publishId", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStatusIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DurableEventPublishResult(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                durableOperationId: null,
                (DurableEventPublishStatus)999));
    }
}
