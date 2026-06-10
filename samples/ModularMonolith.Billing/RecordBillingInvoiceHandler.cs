using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence.Postgres.Persistence;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;
using Dapper;

namespace Bondstone.Samples.ModularMonolith.Billing;

public sealed class RecordBillingInvoiceHandler(IPostgresModuleSession session)
    : IIntegrationEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(
        OrderPlacedEvent integrationEvent,
        CancellationToken ct = default)
    {
        await session.EnsureOpenAsync(ct);
        await session.Connection.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO billing.invoices (
                "Id", "OrderId", "Sku", "Quantity"
            )
            VALUES (
                @Id, @OrderId, @Sku, @Quantity
            )
            """,
            new
            {
                Id = Guid.NewGuid(),
                integrationEvent.OrderId,
                integrationEvent.Sku,
                integrationEvent.Quantity,
            },
            session.Transaction,
            cancellationToken: ct));
    }
}
