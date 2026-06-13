using Bondstone.Modules;
using Bondstone.Persistence.Postgres.Persistence;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;

namespace Bondstone.Samples.ModularMonolith.Billing;

public sealed class BillingBondstoneModule : IBondstoneModule
{
    public BillingBondstoneModule(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionString = connectionString;
    }

    private readonly string _connectionString;

    public string Name => BillingModule.ModuleName;

    public void Configure(BondstoneModuleBuilder module)
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UseDurableMessaging();
        module.UsePostgresPersistence(
            _connectionString,
            schema: BillingModule.ModuleName);
        module.Events.RegisterSubscriber<OrderPlacedEvent, RecordBillingInvoiceHandler>(
            "billing.order-invoice-projection.v1");
    }
}
