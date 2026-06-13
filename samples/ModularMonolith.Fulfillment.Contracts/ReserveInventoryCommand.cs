using Bondstone.Messaging;

namespace Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;

[DurableCommandIdentity("fulfillment.inventory.reserve.v1")]
public sealed record ReserveInventoryCommand(
    Guid OrderId,
    string Sku,
    int Quantity)
    : IDurableCommand;
