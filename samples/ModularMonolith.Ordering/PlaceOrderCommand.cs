using Bondstone.Messaging;

namespace Bondstone.Samples.ModularMonolith.Ordering;

public sealed record PlaceOrderCommand(
    Guid OrderId,
    string Sku,
    int Quantity,
    Guid DurableOperationId)
    : ICommand<PlaceOrderResult>;

public sealed record PlaceOrderResult(
    Guid OrderId,
    DurableOperationHandle ReservationOperation);
