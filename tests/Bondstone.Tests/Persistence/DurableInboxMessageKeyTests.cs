using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableInboxMessageKeyTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_NormalizesTextFields()
    {
        Guid messageId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var key = new DurableInboxMessageKey(
            messageId,
            " sales ",
            " sales.customer.registered.v1 ");

        Assert.Equal(messageId, key.MessageId);
        Assert.Equal("sales", key.ModuleName);
        Assert.Equal("sales.customer.registered.v1", key.HandlerIdentity);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenMessageIdIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateKey(messageId: Guid.Empty));

        Assert.Equal("messageId", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenModuleNameIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateKey(moduleName: " "));

        Assert.Equal("moduleName", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenHandlerIdentityIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateKey(handlerIdentity: " "));

        Assert.Equal("handlerIdentity", exception.ParamName);
    }

    private static DurableInboxMessageKey CreateKey(
        Guid? messageId = null,
        string moduleName = "sales",
        string handlerIdentity = "sales.customer.registered.v1")
    {
        return new DurableInboxMessageKey(
            messageId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            moduleName,
            handlerIdentity);
    }
}
