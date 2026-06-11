using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class ReserveInventoryHandler(
    FulfillmentDbContext dbContext,
    IDurableEventPublisher eventPublisher)
    : ICommandHandler<ReserveInventoryCommand>
{
    public async ValueTask HandleAsync(
        ReserveInventoryCommand command,
        CancellationToken ct = default)
    {
        dbContext.Reservations.Add(FulfillmentReservation.Reserve(
            command.OrderId,
            command.Sku,
            command.Quantity));

        await eventPublisher.PublishAsync(
            new InventoryReservedEvent(
                command.OrderId,
                command.Sku,
                command.Quantity),
            partitionKey: command.OrderId.ToString("D"),
            ct: ct);
    }
}
