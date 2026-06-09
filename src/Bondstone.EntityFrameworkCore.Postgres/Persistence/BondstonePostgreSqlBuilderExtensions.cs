using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Bondstone.EntityFrameworkCore.Postgres.Persistence;

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

        builder.Services.AddBondstonePostgreSqlPersistence<TDbContext>(
            connectionString,
            configureNpgsql,
            schema);
        builder.Services.AddBondstonePostgreSqlModulePersistence<TDbContext>(
            moduleName,
            schema);
        builder.Outbox.MarkPersistenceProvider("PostgreSQL");

        return builder;
    }

    public static BondstoneModuleBuilder UsePostgreSqlPersistence<TDbContext>(
        this BondstoneModuleBuilder module,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null,
        string? schema = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(module);

        module.UseEntityFrameworkCorePersistence<TDbContext>();
        module.Services.AddBondstonePostgreSqlPersistence<TDbContext>(
            connectionString,
            configureNpgsql,
            schema);
        module.Services.AddBondstonePostgreSqlModulePersistence<TDbContext>(
            module.Name,
            schema);
        module.UseOutboxPersistenceProvider("PostgreSQL");

        return module;
    }
}
