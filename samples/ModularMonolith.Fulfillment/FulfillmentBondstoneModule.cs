using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class FulfillmentBondstoneModule : IBondstoneModule
{
    public FulfillmentBondstoneModule(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionString = connectionString;
    }

    private readonly string _connectionString;

    public string Name => FulfillmentModule.ModuleName;

    public void Configure(BondstoneModuleBuilder module)
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UseDurableMessaging();
        module.UsePostgreSqlPersistence<FulfillmentDbContext>(
            _connectionString,
            schema: FulfillmentModule.ModuleName);
        module.Commands.RegisterFromAssemblyContaining<ReserveInventoryHandler>();
        module.Events.RegisterPublishedEvent<InventoryReservedEvent>();
        module.Events.RegisterSubscriber<OrderPlacedEvent, RecordOrderPlacedHandler>(
            "fulfillment.order-placed-projection.v1");
    }
}
