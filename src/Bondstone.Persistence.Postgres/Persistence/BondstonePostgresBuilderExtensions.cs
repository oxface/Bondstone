using Bondstone.Configuration;
using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Persistence.Postgres.Persistence;

public static class BondstonePostgresBuilderExtensions
{
    public static BondstoneModuleBuilder UsePostgresPersistence(
        this BondstoneModuleBuilder module,
        string connectionString,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UsePersistence(PostgresModulePersistence.ProviderName);
        module.Services.AddBondstonePostgresPersistence(connectionString);
        module.Services.AddBondstonePostgresModulePersistence(module.Name, schema);
        module.TryAddPostgresModuleTransactionSystemBehaviors();
        module.UseOutboxPersistenceProvider(PostgresModulePersistence.ProviderName);

        return module;
    }

    public static BondstoneBuilder UsePostgresPersistence(
        this BondstoneBuilder builder,
        string moduleName,
        string connectionString,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Module(
            moduleName,
            module => module.UsePostgresPersistence(
                connectionString,
                schema));
    }

    private static void TryAddPostgresModuleTransactionSystemBehaviors(
        this BondstoneModuleBuilder module)
    {
        module.Services.TryAddScoped(serviceProvider =>
            new PostgresModuleRuntimeRegistry(
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>()));
        module.AddCommandPipelineContribution(
            new ModuleCommandPipelineContribution(
                "Bondstone.Persistence.Postgres.Command.Transaction",
                ModulePipelineStepKind.System,
                ModuleCommandSystemPipelineOrder.Transaction,
                typeof(PostgresModuleTransactionBehavior<>),
                UsesPostgresPersistence));
        module.AddEventSubscriberPipelineContribution(
            new ModuleEventSubscriberPipelineContribution(
                "Bondstone.Persistence.Postgres.EventSubscriber.Transaction",
                ModulePipelineStepKind.System,
                ModuleEventSubscriberSystemPipelineOrder.Transaction,
                typeof(PostgresModuleEventSubscriberTransactionBehavior<>),
                UsesPostgresPersistence));
    }

    private static bool UsesPostgresPersistence(BondstoneModuleRegistration module)
    {
        return module.UsesPersistence
            && StringComparer.Ordinal.Equals(
                module.PersistenceProviderName,
                PostgresModulePersistence.ProviderName);
    }
}
