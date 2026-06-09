namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class FulfillmentOrderEvent
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; }
}
