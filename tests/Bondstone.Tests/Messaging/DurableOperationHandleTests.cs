using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableOperationHandleTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_NormalizesModuleNames()
    {
        Guid durableOperationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var handle = new DurableOperationHandle(
            durableOperationId,
            " sales ",
            " fulfillment ");

        Assert.Equal(durableOperationId, handle.DurableOperationId);
        Assert.Equal("sales", handle.SourceModule);
        Assert.Equal("fulfillment", handle.TargetModule);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenOperationIdIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new DurableOperationHandle(
                Guid.Empty,
                "sales",
                "fulfillment"));

        Assert.Equal("durableOperationId", exception.ParamName);
    }

    [Theory]
    [InlineData(null, "fulfillment", "sourceModule")]
    [InlineData("sales", null, "targetModule")]
    [Trait("Category", "Unit")]
    public void Constructor_WhenModuleNameIsMissing_Throws(
        string? sourceModule,
        string? targetModule,
        string expectedParameter)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new DurableOperationHandle(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                sourceModule!,
                targetModule!));

        Assert.Equal(expectedParameter, exception.ParamName);
    }
}
