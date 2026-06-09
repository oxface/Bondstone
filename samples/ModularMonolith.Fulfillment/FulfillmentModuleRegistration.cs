using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public static class FulfillmentModuleRegistration
{
    public static BondstoneBuilder AddFulfillmentModule(
        this BondstoneBuilder bondstone,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(bondstone);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return bondstone.Module(FulfillmentModule.Name, module =>
        {
            module.UseDurableMessaging();
            module.UsePostgreSqlPersistence<FulfillmentDbContext>(
                connectionString,
                schema: FulfillmentModule.Name);
            module.Commands.RegisterFromAssemblyContaining<ReserveInventoryHandler>();
        });
    }
}
