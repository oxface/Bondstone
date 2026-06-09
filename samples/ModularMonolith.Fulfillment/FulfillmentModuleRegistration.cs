using Bondstone.Configuration;
using Bondstone.Modules;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public static class FulfillmentModuleRegistration
{
    public static BondstoneBuilder AddFulfillmentModule(
        this BondstoneBuilder bondstone,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(bondstone);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return bondstone.AddModule(new FulfillmentBondstoneModule(connectionString));
    }
}
