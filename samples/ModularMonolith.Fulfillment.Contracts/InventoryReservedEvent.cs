using Bondstone.Messaging;

namespace Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;

[IntegrationEventIdentity(FulfillmentIntegrationEvents.InventoryReserved)]
public sealed record InventoryReservedEvent(
    Guid OrderId,
    string Sku,
    int Quantity)
    : IIntegrationEvent;
