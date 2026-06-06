using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.EntityFrameworkCore.Persistence;

public static class BondstoneEntityFrameworkCoreModuleBuilderExtensions
{
    public static BondstoneModuleBuilder UseEntityFrameworkCorePersistence<TDbContext>(
        this BondstoneModuleBuilder module)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UsePersistence(
            EntityFrameworkCoreModulePersistence.ProviderName,
            typeof(TDbContext));
        module.Services.AddBondstoneEntityFrameworkCorePersistence<TDbContext>();
        module.Services.TryAddEntityFrameworkCoreModuleTransactionSystemBehavior();

        return module;
    }

    private static void TryAddEntityFrameworkCoreModuleTransactionSystemBehavior(
        this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandSystemPipelineBehavior<>),
            typeof(EntityFrameworkCoreModuleTransactionBehavior<>)));
    }
}
