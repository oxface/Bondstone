namespace Bondstone.Samples.ModularMonolith.Ordering;

public sealed class Order
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = "";

    public int Quantity { get; set; }
}
