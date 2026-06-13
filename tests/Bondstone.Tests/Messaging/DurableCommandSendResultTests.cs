using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableCommandSendResultTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_CarriesSendState()
    {
        Guid sendId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid durableOperationId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var result = new DurableCommandSendResult(
            sendId,
            durableOperationId,
            DurableCommandSendStatus.Accepted);

        Assert.Equal(sendId, result.SendId);
        Assert.Equal(durableOperationId, result.DurableOperationId);
        Assert.Equal(DurableCommandSendStatus.Accepted, result.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenSendIdIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new DurableCommandSendResult(
                Guid.Empty,
                durableOperationId: null,
                DurableCommandSendStatus.Accepted));

        Assert.Equal("sendId", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenStatusIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new DurableCommandSendResult(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                durableOperationId: null,
                (DurableCommandSendStatus)999));
    }
}
