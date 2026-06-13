using Bondstone.Configuration;
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

        return bondstone.AddModule(new OrderingBondstoneModule(connectionString));
    }
}
