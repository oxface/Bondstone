using Bondstone.DomainEvents;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class FulfillmentReservation : IDomainEventSource
{
    private readonly List<IDomainEvent> _pendingDomainEvents = [];

    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Sku { get; set; } = "";

    public int Quantity { get; set; }

    public IReadOnlyCollection<IDomainEvent> PendingDomainEvents => _pendingDomainEvents;

    public static FulfillmentReservation Reserve(
        Guid orderId,
        string sku,
        int quantity)
    {
        var reservation = new FulfillmentReservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Sku = sku,
            Quantity = quantity,
        };

        reservation._pendingDomainEvents.Add(new InventoryReservedDomainEvent(
            orderId,
            sku,
            quantity));

        return reservation;
    }

    public void ClearPendingDomainEvents()
    {
        _pendingDomainEvents.Clear();
    }
}

[DomainEventIdentity("fulfillment.inventory-reservation-recorded.v1")]
public sealed record InventoryReservedDomainEvent(
    Guid OrderId,
    string Sku,
    int Quantity) : IDomainEvent;
