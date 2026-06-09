namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class FulfillmentReservation
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Sku { get; set; } = "";

    public int Quantity { get; set; }
}
