using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;

namespace Bondstone.Samples.ModularMonolith.Ordering;

public sealed class OrderingBondstoneModule : IBondstoneModule
{
    public OrderingBondstoneModule(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        _connectionString = connectionString;
    }

    private readonly string _connectionString;

    public string Name => OrderingModule.ModuleName;

    public void Configure(BondstoneModuleBuilder module)
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UseDurableMessaging();
        module.UsePostgreSqlPersistence<OrderingDbContext>(
            _connectionString,
            schema: OrderingModule.ModuleName);
        module.Commands.RegisterFromAssemblyContaining<PlaceOrderHandler>();
        module.Events.RegisterPublishedEvent<OrderPlacedEvent>();
    }
}
