using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;

namespace Bondstone.Samples.ModularMonolith.Billing;

public sealed class RecordBillingInvoiceHandler(BillingDbContext dbContext)
    : IIntegrationEventHandler<OrderPlacedEvent>
{
    public ValueTask HandleAsync(
        OrderPlacedEvent integrationEvent,
        CancellationToken ct = default)
    {
        dbContext.Invoices.Add(new BillingInvoice
        {
            Id = Guid.NewGuid(),
            OrderId = integrationEvent.OrderId,
            Sku = integrationEvent.Sku,
            Quantity = integrationEvent.Quantity,
        });

        return ValueTask.CompletedTask;
    }
}
