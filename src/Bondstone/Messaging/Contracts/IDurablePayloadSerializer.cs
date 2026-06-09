namespace Bondstone.Messaging;

public interface IDurablePayloadSerializer
{
    string Serialize<TMessage>(TMessage message)
        where TMessage : IMessage;

    object Deserialize(
        string payload,
        Type messageType);
}
