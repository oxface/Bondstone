using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class ReserveInventoryHandler(FulfillmentDbContext dbContext)
    : ICommandHandler<ReserveInventoryCommand>
{
    public ValueTask HandleAsync(
        ReserveInventoryCommand command,
        CancellationToken ct = default)
    {
        dbContext.Reservations.Add(new FulfillmentReservation
        {
            Id = Guid.NewGuid(),
            OrderId = command.OrderId,
            Sku = command.Sku,
            Quantity = command.Quantity,
        });

        return ValueTask.CompletedTask;
    }
}
