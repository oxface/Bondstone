using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

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
        module.Services.TryAddEntityFrameworkCoreModuleTransactionSystemBehaviors();

        return module;
    }

    private static void TryAddEntityFrameworkCoreModuleTransactionSystemBehaviors(
        this IServiceCollection services)
    {
        services.TryAddScoped(serviceProvider =>
            new EntityFrameworkCoreModuleRuntimeRegistry(
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>()));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandSystemPipelineBehavior<>),
            typeof(EntityFrameworkCoreModuleTransactionBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleEventSubscriberSystemPipelineBehavior<>),
            typeof(EntityFrameworkCoreModuleEventSubscriberTransactionBehavior<>)));
    }
}
