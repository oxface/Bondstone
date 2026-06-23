using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bondstone.Messaging;

public sealed class SystemTextJsonDurableMessageEnvelopeSerializer
    : IDurableMessageEnvelopeSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateOptions();

    public string Serialize(
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return JsonSerializer.Serialize(envelope, SerializerOptions);
    }

    public byte[] SerializeToUtf8Bytes(
        DurableMessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return JsonSerializer.SerializeToUtf8Bytes(envelope, SerializerOptions);
    }

    public DurableMessageEnvelope Deserialize(
        string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("Envelope JSON is required.", nameof(json));
        }

        return JsonSerializer.Deserialize<DurableMessageEnvelope>(
                json,
                SerializerOptions)
            ?? throw new InvalidOperationException(
                "Envelope JSON did not contain a durable message envelope.");
    }

    public DurableMessageEnvelope Deserialize(
        ReadOnlyMemory<byte> utf8Json)
    {
        if (utf8Json.IsEmpty)
        {
            throw new ArgumentException("Envelope JSON is required.", nameof(utf8Json));
        }

        return JsonSerializer.Deserialize<DurableMessageEnvelope>(
                utf8Json.Span,
                SerializerOptions)
            ?? throw new InvalidOperationException(
                "Envelope JSON did not contain a durable message envelope.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter<MessageKind>());
        return options;
    }
}
