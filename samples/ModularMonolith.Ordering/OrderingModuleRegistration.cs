using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Modules;

namespace Bondstone.Samples.ModularMonolith.Ordering;

public static class OrderingModuleRegistration
{
    public static BondstoneBuilder AddOrderingModule(
        this BondstoneBuilder bondstone,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(bondstone);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return bondstone.Module(OrderingModule.Name, module =>
        {
            module.UseDurableMessaging();
            module.UsePostgreSqlPersistence<OrderingDbContext>(
                connectionString,
                schema: OrderingModule.Name);
            module.Commands.RegisterFromAssemblyContaining<PlaceOrderHandler>();
        });
    }
}
