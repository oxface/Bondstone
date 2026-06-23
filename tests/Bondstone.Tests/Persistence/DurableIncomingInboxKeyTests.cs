using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableIncomingInboxKeyTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenValuesAreValid_NormalizesTextFields()
    {
        Guid messageId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var key = new DurableIncomingInboxKey(
            messageId,
            " fulfillment ",
            " fulfillment.reserve-inventory.v1 ");

        Assert.Equal(messageId, key.MessageId);
        Assert.Equal("fulfillment", key.ReceiverModule);
        Assert.Equal("fulfillment.reserve-inventory.v1", key.HandlerIdentity);
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
    public void Constructor_WhenReceiverModuleIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateKey(receiverModule: " "));

        Assert.Equal("receiverModule", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenHandlerIdentityIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateKey(handlerIdentity: " "));

        Assert.Equal("handlerIdentity", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ForCommandHandler_WhenCalled_UsesTargetModuleAndHandlerIdentity()
    {
        Guid messageId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        DurableIncomingInboxKey key = DurableIncomingInboxKey.ForCommandHandler(
            messageId,
            " fulfillment ",
            " fulfillment.reserve-inventory.v1 ");

        Assert.Equal(messageId, key.MessageId);
        Assert.Equal("fulfillment", key.ReceiverModule);
        Assert.Equal("fulfillment.reserve-inventory.v1", key.HandlerIdentity);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ForEventSubscriber_WhenCalled_UsesSubscriberModuleAndIdentity()
    {
        Guid messageId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        DurableIncomingInboxKey key = DurableIncomingInboxKey.ForEventSubscriber(
            messageId,
            " billing ",
            " billing.order-projection.v1 ");

        Assert.Equal(messageId, key.MessageId);
        Assert.Equal("billing", key.ReceiverModule);
        Assert.Equal("billing.order-projection.v1", key.HandlerIdentity);
    }

    private static DurableIncomingInboxKey CreateKey(
        Guid? messageId = null,
        string receiverModule = "fulfillment",
        string handlerIdentity = "fulfillment.reserve-inventory.v1")
    {
        return new DurableIncomingInboxKey(
            messageId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            receiverModule,
            handlerIdentity);
    }
}
