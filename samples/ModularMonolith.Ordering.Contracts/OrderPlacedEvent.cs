using Bondstone.Messaging;

namespace Bondstone.Samples.ModularMonolith.Ordering.Contracts;

[IntegrationEventIdentity(OrderingIntegrationEvents.OrderPlaced)]
public sealed record OrderPlacedEvent(
    Guid OrderId,
    string Sku,
    int Quantity)
    : IIntegrationEvent;
