using Bondstone.Configuration;
using Bondstone.Modules;

namespace Bondstone.Samples.ModularMonolith.Billing;

public static class BillingModuleRegistration
{
    public static BondstoneBuilder AddBillingModule(
        this BondstoneBuilder bondstone,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(bondstone);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        return bondstone.AddModule(new BillingBondstoneModule(connectionString));
    }
}
