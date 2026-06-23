using System.Text;
using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableMessageEnvelopeSerializerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Serialize_WhenEnvelopeRoundTrips_PreservesDurableFields()
    {
        IDurableMessageEnvelopeSerializer serializer =
            new SystemTextJsonDurableMessageEnvelopeSerializer();
        DurableMessageEnvelope envelope = CreateEnvelope();

        string json = serializer.Serialize(envelope);
        DurableMessageEnvelope deserialized = serializer.Deserialize(json);

        Assert.Equal(envelope, deserialized);
        Assert.Contains("\"messageKind\":\"Command\"", json, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Deserialize_WhenUtf8JsonRoundTrips_PreservesDurableFields()
    {
        IDurableMessageEnvelopeSerializer serializer =
            new SystemTextJsonDurableMessageEnvelopeSerializer();
        DurableMessageEnvelope envelope = CreateEnvelope();
        byte[] bytes = serializer.SerializeToUtf8Bytes(envelope);

        DurableMessageEnvelope deserialized = serializer.Deserialize(bytes);

        Assert.Equal(envelope, deserialized);
        Assert.Contains(
            "\"messageTypeName\":\"fulfillment.inventory.reserve.v1\"",
            Encoding.UTF8.GetString(bytes),
            StringComparison.Ordinal);
    }

    private static DurableMessageEnvelope CreateEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Command,
            "fulfillment.inventory.reserve.v1",
            "ordering",
            "fulfillment",
            """{"orderId":"order-1"}""",
            DateTimeOffset.Parse("2026-06-16T00:00:00+00:00"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            new MessageTraceContext(
                "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
                "state",
                "user=abc"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            "order-1",
            """{"tenant":"default"}""");
    }
}
