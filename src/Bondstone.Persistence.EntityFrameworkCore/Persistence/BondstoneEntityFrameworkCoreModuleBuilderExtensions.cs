using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

/// <summary>
/// Adds provider-neutral EF Core durable persistence to Bondstone modules.
/// </summary>
public static class BondstoneEntityFrameworkCoreModuleBuilderExtensions
{
    /// <summary>
    /// Configures a module to use EF Core durable persistence and registers the root EF Core persistence services.
    /// </summary>
    /// <typeparam name="TDbContext">The module DbContext type that contains Bondstone durable mappings.</typeparam>
    /// <param name="module">The module builder.</param>
    /// <returns>The same module builder for chained setup.</returns>
    public static BondstoneModuleBuilder UseEntityFrameworkCorePersistence<TDbContext>(
        this BondstoneModuleBuilder module)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UseEntityFrameworkCoreModulePersistence<TDbContext>();
        module.Services.AddBondstoneEntityFrameworkCorePersistence<TDbContext>();

        return module;
    }

    /// <summary>
    /// Marks a module as using EF Core persistence and adds EF Core transaction runtime behavior.
    /// </summary>
    /// <typeparam name="TDbContext">The module DbContext type used for the module transaction.</typeparam>
    /// <param name="module">The module builder.</param>
    /// <returns>The same module builder for chained setup.</returns>
    public static BondstoneModuleBuilder UseEntityFrameworkCoreModulePersistence<TDbContext>(
        this BondstoneModuleBuilder module)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UsePersistence(
            EntityFrameworkCoreModulePersistence.ProviderName,
            typeof(TDbContext));
        module.TryAddEntityFrameworkCoreModuleTransactionSystemBehaviors();

        return module;
    }

    private static void TryAddEntityFrameworkCoreModuleTransactionSystemBehaviors(
        this BondstoneModuleBuilder module)
    {
        module.Services.TryAddScoped(serviceProvider =>
            new EntityFrameworkCoreModuleRuntimeRegistry(
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>()));
        module.Services.TryAddEnumerable(
            ServiceDescriptor.Scoped(
                typeof(IModuleTransactionRunner),
                typeof(EntityFrameworkCoreModuleTransactionRunner)));
    }
}
