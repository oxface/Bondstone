using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed record DurableInboxMessageKey
{
    public DurableInboxMessageKey(
        Guid messageId,
        string moduleName,
        string handlerIdentity)
    {
        if (messageId == Guid.Empty)
        {
            throw new ArgumentException("Message id must not be empty.", nameof(messageId));
        }

        MessageId = messageId;
        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        HandlerIdentity = handlerIdentity.NormalizeRequired(nameof(handlerIdentity), "Handler identity");
    }

    public Guid MessageId { get; }

    public string ModuleName { get; }

    public string HandlerIdentity { get; }

    public static DurableInboxMessageKey ForCommandHandler(
        Guid messageId,
        string moduleName,
        string handlerIdentity)
    {
        return new DurableInboxMessageKey(
            messageId,
            moduleName,
            handlerIdentity);
    }

    public static DurableInboxMessageKey ForEventSubscriber(
        Guid messageId,
        string subscriberModule,
        string subscriberIdentity)
    {
        return new DurableInboxMessageKey(
            messageId,
            subscriberModule,
            subscriberIdentity);
    }
}
