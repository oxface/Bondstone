using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Configuration;

public static class BondstoneServiceCollectionExtensions
{
    public static IServiceCollection AddBondstone(
        this IServiceCollection services,
        Action<BondstoneBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new BondstoneBuilder(services);
        configure(builder);
        builder.Validate();

        return services;
    }
}
