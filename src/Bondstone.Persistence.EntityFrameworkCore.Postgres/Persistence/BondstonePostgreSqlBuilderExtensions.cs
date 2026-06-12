using Bondstone.Configuration;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;

public static class BondstonePostgreSqlBuilderExtensions
{
    public static BondstoneBuilder UsePostgreSqlPersistence<TDbContext>(
        this BondstoneBuilder builder,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null,
        string? schema = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddBondstonePostgreSqlPersistence<TDbContext>(
            connectionString,
            configureNpgsql,
            schema);
        builder.Outbox.MarkPersistenceProvider("PostgreSQL");

        return builder;
    }

    public static BondstoneBuilder UsePostgreSqlPersistence<TDbContext>(
        this BondstoneBuilder builder,
        string moduleName,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null,
        string? schema = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.Module(
            moduleName,
            module => module.UsePostgreSqlPersistence<TDbContext>(
                connectionString,
                configureNpgsql,
                schema));
    }

    public static BondstoneModuleBuilder UsePostgreSqlPersistence<TDbContext>(
        this BondstoneModuleBuilder module,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null,
        string? schema = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UseEntityFrameworkCoreModulePersistence<TDbContext>();
        module.Services.AddBondstonePostgreSqlModuleInfrastructure<TDbContext>(
            connectionString,
            configureNpgsql);
        module.Services.AddBondstonePostgreSqlModulePersistence<TDbContext>(
            module.Name,
            schema);
        module.UseOutboxPersistenceProvider("PostgreSQL");

        return module;
    }
}
