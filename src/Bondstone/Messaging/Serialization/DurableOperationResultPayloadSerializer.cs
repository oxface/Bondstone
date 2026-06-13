using System.Text.Json;

namespace Bondstone.Messaging;

internal sealed class DurableOperationResultPayloadSerializer(
    DurablePayloadJsonOptions? options = null)
{
    private readonly JsonSerializerOptions _jsonSerializerOptions =
        options?.JsonSerializerOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    public string Serialize<TResult>(TResult? result)
    {
        return JsonSerializer.Serialize(
            result,
            typeof(TResult),
            _jsonSerializerOptions);
    }

    public TResult? Deserialize<TResult>(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return JsonSerializer.Deserialize<TResult>(
            payload,
            _jsonSerializerOptions);
    }
}
