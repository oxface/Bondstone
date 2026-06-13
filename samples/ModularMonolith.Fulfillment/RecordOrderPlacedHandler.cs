using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class RecordOrderPlacedHandler(FulfillmentDbContext dbContext)
    : IIntegrationEventHandler<OrderPlacedEvent>
{
    public ValueTask HandleAsync(
        OrderPlacedEvent integrationEvent,
        CancellationToken ct = default)
    {
        dbContext.OrderEvents.Add(new FulfillmentOrderEvent
        {
            Id = Guid.NewGuid(),
            OrderId = integrationEvent.OrderId,
            Sku = integrationEvent.Sku,
            Quantity = integrationEvent.Quantity,
        });

        return ValueTask.CompletedTask;
    }
}
