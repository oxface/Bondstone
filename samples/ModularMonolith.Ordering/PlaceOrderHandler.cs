using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;

namespace Bondstone.Samples.ModularMonolith.Ordering;

public sealed class PlaceOrderHandler(
    OrderingDbContext dbContext,
    IDurableCommandSender commandSender,
    IDurableEventPublisher eventPublisher)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command,
        CancellationToken ct = default)
    {
        dbContext.Orders.Add(new Order
        {
            Id = command.OrderId,
            Sku = command.Sku,
            Quantity = command.Quantity,
        });

        DurableCommandSendResult reservationSend = await commandSender.SendAsync(
            new ReserveInventoryCommand(
                command.OrderId,
                command.Sku,
                command.Quantity),
            FulfillmentModule.ModuleName,
            partitionKey: command.OrderId.ToString("D"),
            durableOperationId: command.DurableOperationId,
            ct: ct);

        await eventPublisher.PublishAsync(
            new OrderPlacedEvent(
                command.OrderId,
                command.Sku,
                command.Quantity),
            partitionKey: command.OrderId.ToString("D"),
            durableOperationId: command.DurableOperationId,
            ct: ct);

        return new PlaceOrderResult(
            command.OrderId,
            reservationSend.Operation
            ?? throw new InvalidOperationException(
                "Reservation command send did not return a durable operation handle."));
    }
}
