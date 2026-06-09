using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;

namespace Bondstone.Samples.ModularMonolith.Ordering;

public sealed class RecordInventoryReservedHandler(OrderingDbContext dbContext)
    : IIntegrationEventHandler<InventoryReservedEvent>
{
    public ValueTask HandleAsync(
        InventoryReservedEvent integrationEvent,
        CancellationToken ct = default)
    {
        dbContext.InventoryReservations.Add(new OrderInventoryReservation
        {
            Id = Guid.NewGuid(),
            OrderId = integrationEvent.OrderId,
            Sku = integrationEvent.Sku,
            Quantity = integrationEvent.Quantity,
        });

        return ValueTask.CompletedTask;
    }
}
