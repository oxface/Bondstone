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
        module.Services.TryAddPostgresModuleTransactionSystemBehaviors();
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

        builder.Services.AddBondstonePostgresPersistence(connectionString);
        builder.Services.AddBondstonePostgresModulePersistence(moduleName, schema);
        builder.Outbox.MarkPersistenceProvider(PostgresModulePersistence.ProviderName);

        return builder;
    }

    private static void TryAddPostgresModuleTransactionSystemBehaviors(
        this IServiceCollection services)
    {
        services.TryAddScoped(serviceProvider =>
            new PostgresModuleRuntimeRegistry(
                serviceProvider.GetRequiredService<IBondstoneModuleRegistry>()));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandSystemPipelineBehavior<>),
            typeof(PostgresModuleTransactionBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleEventSubscriberSystemPipelineBehavior<>),
            typeof(PostgresModuleEventSubscriberTransactionBehavior<>)));
    }
}
