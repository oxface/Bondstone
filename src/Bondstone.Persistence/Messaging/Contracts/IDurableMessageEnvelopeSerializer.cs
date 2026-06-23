namespace Bondstone.Messaging;

public interface IDurableMessageEnvelopeSerializer
{
    string Serialize(
        DurableMessageEnvelope envelope);

    byte[] SerializeToUtf8Bytes(
        DurableMessageEnvelope envelope);

    DurableMessageEnvelope Deserialize(
        string json);

    DurableMessageEnvelope Deserialize(
        ReadOnlyMemory<byte> utf8Json);
}
