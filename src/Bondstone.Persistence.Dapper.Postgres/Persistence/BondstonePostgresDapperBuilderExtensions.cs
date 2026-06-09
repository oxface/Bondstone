using Bondstone.Configuration;
using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Persistence.Dapper.Postgres.Persistence;

public static class BondstonePostgresDapperBuilderExtensions
{
    public static BondstoneModuleBuilder UsePostgresDapperPersistence(
        this BondstoneModuleBuilder module,
        string connectionString,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UsePersistence(PostgresDapperModulePersistence.ProviderName);
        module.Services.AddBondstonePostgresDapperPersistence(connectionString);
        module.Services.AddBondstonePostgresDapperModulePersistence(module.Name, schema);
        module.Services.TryAddPostgresDapperModuleTransactionSystemBehaviors();
        module.UseOutboxPersistenceProvider("PostgreSQL.Dapper");

        return module;
    }

    public static BondstoneBuilder UsePostgresDapperPersistence(
        this BondstoneBuilder builder,
        string moduleName,
        string connectionString,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddBondstonePostgresDapperPersistence(connectionString);
        builder.Services.AddBondstonePostgresDapperModulePersistence(moduleName, schema);
        builder.Outbox.MarkPersistenceProvider("PostgreSQL.Dapper");

        return builder;
    }

    private static void TryAddPostgresDapperModuleTransactionSystemBehaviors(
        this IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleCommandSystemPipelineBehavior<>),
            typeof(PostgresDapperModuleTransactionBehavior<>)));
        services.TryAddEnumerable(ServiceDescriptor.Scoped(
            typeof(IModuleEventSubscriberSystemPipelineBehavior<>),
            typeof(PostgresDapperModuleEventSubscriberTransactionBehavior<>)));
    }
}
