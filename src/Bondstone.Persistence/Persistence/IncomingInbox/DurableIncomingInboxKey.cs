using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed record DurableIncomingInboxKey
{
    public DurableIncomingInboxKey(
        Guid messageId,
        string receiverModule,
        string handlerIdentity)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message id must not be empty.", nameof(messageId));
        }

        MessageId = messageId;
        ReceiverModule = receiverModule.NormalizeRequired(nameof(receiverModule), "Receiver module");
        HandlerIdentity = handlerIdentity.NormalizeRequired(nameof(handlerIdentity), "Handler identity");
    }

    public Guid MessageId { get; }

    public string ReceiverModule { get; }

    public string HandlerIdentity { get; }

    public static DurableIncomingInboxKey ForCommandHandler(
        Guid messageId,
        string targetModule,
        string handlerIdentity)
    {
        return new DurableIncomingInboxKey(
            messageId,
            targetModule,
            handlerIdentity);
    }

    public static DurableIncomingInboxKey ForEventSubscriber(
        Guid messageId,
        string subscriberModule,
        string subscriberIdentity)
    {
        return new DurableIncomingInboxKey(
            messageId,
            subscriberModule,
            subscriberIdentity);
    }
}
