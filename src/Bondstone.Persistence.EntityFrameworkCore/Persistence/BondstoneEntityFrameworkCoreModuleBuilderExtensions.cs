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

        module.UseEntityFrameworkCoreModulePersistence<TDbContext>();
        module.Services.AddBondstoneEntityFrameworkCorePersistence<TDbContext>();

        return module;
    }

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
        module.AddCommandPipelineContribution(
            new ModuleCommandPipelineContribution(
                "Bondstone.Persistence.EntityFrameworkCore.Command.Transaction",
                ModulePipelineStepKind.System,
                ModuleCommandSystemPipelineOrder.Transaction,
                typeof(EntityFrameworkCoreModuleTransactionBehavior<>),
                UsesEntityFrameworkCorePersistence));
        module.AddEventSubscriberPipelineContribution(
            new ModuleEventSubscriberPipelineContribution(
                "Bondstone.Persistence.EntityFrameworkCore.EventSubscriber.Transaction",
                ModulePipelineStepKind.System,
                ModuleEventSubscriberSystemPipelineOrder.Transaction,
                typeof(EntityFrameworkCoreModuleEventSubscriberTransactionBehavior<>),
                UsesEntityFrameworkCorePersistence));
    }

    private static bool UsesEntityFrameworkCorePersistence(BondstoneModuleRegistration module)
    {
        return module.UsesPersistence
            && StringComparer.Ordinal.Equals(
                module.PersistenceProviderName,
                EntityFrameworkCoreModulePersistence.ProviderName);
    }
}
