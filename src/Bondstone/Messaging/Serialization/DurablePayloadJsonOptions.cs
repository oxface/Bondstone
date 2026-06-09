using System.Text.Json;

namespace Bondstone.Messaging;

public sealed class DurablePayloadJsonOptions
{
    public JsonSerializerOptions JsonSerializerOptions { get; } =
        new(JsonSerializerDefaults.Web);
}
