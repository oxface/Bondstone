using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;

namespace Bondstone.Samples.ModularMonolith.Ordering;

public sealed class PlaceOrderHandler(
    OrderingDbContext dbContext,
    IDurableCommandSender commandSender)
    : ICommandHandler<PlaceOrderCommand>
{
    public async ValueTask HandleAsync(
        PlaceOrderCommand command,
        CancellationToken ct = default)
    {
        dbContext.Orders.Add(new Order
        {
            Id = command.OrderId,
            Sku = command.Sku,
            Quantity = command.Quantity,
        });

        await commandSender.SendAsync(
            new ReserveInventoryCommand(
                command.OrderId,
                command.Sku,
                command.Quantity),
            FulfillmentModule.Name,
            partitionKey: command.OrderId.ToString("D"),
            durableOperationId: command.DurableOperationId,
            ct: ct);
    }
}
