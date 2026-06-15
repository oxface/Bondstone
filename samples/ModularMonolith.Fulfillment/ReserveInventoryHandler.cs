using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class ReserveInventoryHandler(
    FulfillmentDbContext dbContext,
    IDurableEventPublisher eventPublisher)
    : ICommandHandler<ReserveInventoryCommand, ReserveInventoryResult>
{
    public async ValueTask<ReserveInventoryResult> HandleAsync(
        ReserveInventoryCommand command,
        CancellationToken ct = default)
    {
        FulfillmentReservation reservation = FulfillmentReservation.Reserve(
            command.OrderId,
            command.Sku,
            command.Quantity);
        dbContext.Reservations.Add(reservation);

        await eventPublisher.PublishAsync(
            new InventoryReservedEvent(
                command.OrderId,
                command.Sku,
                command.Quantity),
            partitionKey: command.OrderId.ToString("D"),
            ct: ct);

        return new ReserveInventoryResult(
            reservation.Id,
            command.OrderId,
            command.Sku,
            command.Quantity);
    }
}
