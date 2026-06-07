using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bondstone.Tests.Messaging;

public readonly record struct DurableOrderId(string Value);

internal sealed class DurableOrderIdJsonConverter : JsonConverter<DurableOrderId>
{
    public override DurableOrderId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        string value = reader.GetString()
            ?? throw new JsonException("Order id is required.");

        const string prefix = "payload-";
        return value.StartsWith(prefix, StringComparison.Ordinal)
            ? new DurableOrderId(value[prefix.Length..])
            : new DurableOrderId(value);
    }

    public override void Write(
        Utf8JsonWriter writer,
        DurableOrderId value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue($"payload-{value.Value}");
    }
}
