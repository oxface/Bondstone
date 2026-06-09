using System.Text.Json;

namespace Bondstone.Messaging;

public sealed class SystemTextJsonDurablePayloadSerializer(
    DurablePayloadJsonOptions? options = null)
    : IDurablePayloadSerializer
{
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        options?.JsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public string Serialize<TMessage>(TMessage message)
        where TMessage : IMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        return JsonSerializer.Serialize(message, _jsonSerializerOptions);
    }

    public object Deserialize(
        string payload,
        Type messageType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentNullException.ThrowIfNull(messageType);

        object? message = JsonSerializer.Deserialize(
            payload,
            messageType,
            _jsonSerializerOptions);

        return message
            ?? throw new JsonException(
                $"Message payload for '{messageType.FullName}' deserialized to null.");
    }
}
