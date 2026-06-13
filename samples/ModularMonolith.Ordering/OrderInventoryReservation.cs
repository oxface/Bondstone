namespace Bondstone.Samples.ModularMonolith.Ordering;

public sealed class OrderInventoryReservation
{
    public required Guid Id { get; init; }

    public required Guid OrderId { get; init; }

    public required string Sku { get; init; }

    public required int Quantity { get; init; }
}
