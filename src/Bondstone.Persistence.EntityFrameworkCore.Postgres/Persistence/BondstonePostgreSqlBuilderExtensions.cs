using Bondstone.Configuration;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;

/// <summary>
/// Adds PostgreSQL-backed EF Core durable persistence to Bondstone setup.
/// </summary>
public static class BondstonePostgreSqlBuilderExtensions
{
    /// <summary>
    /// Registers PostgreSQL-backed EF Core durable persistence services for a Bondstone host.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type that contains Bondstone durable mappings.</typeparam>
    /// <param name="builder">The Bondstone host builder.</param>
    /// <param name="connectionString">The PostgreSQL connection string used by the DbContext.</param>
    /// <param name="configureNpgsql">Optional Npgsql provider configuration.</param>
    /// <param name="schema">The optional schema for Bondstone durable tables.</param>
    /// <returns>The same Bondstone builder for chained setup.</returns>
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

    /// <summary>
    /// Configures a named module to use PostgreSQL-backed EF Core durable persistence from the root Bondstone builder.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type that contains Bondstone durable mappings.</typeparam>
    /// <param name="builder">The Bondstone host builder.</param>
    /// <param name="moduleName">The module name to configure.</param>
    /// <param name="connectionString">The PostgreSQL connection string used by the DbContext.</param>
    /// <param name="configureNpgsql">Optional Npgsql provider configuration.</param>
    /// <param name="schema">The optional schema for Bondstone durable tables.</param>
    /// <returns>The same Bondstone builder for chained setup.</returns>
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

    /// <summary>
    /// Configures a module to use PostgreSQL-backed EF Core durable persistence.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type that contains Bondstone durable mappings.</typeparam>
    /// <param name="module">The module builder.</param>
    /// <param name="connectionString">The PostgreSQL connection string used by the DbContext.</param>
    /// <param name="configureNpgsql">Optional Npgsql provider configuration.</param>
    /// <param name="schema">The optional schema for Bondstone durable tables.</param>
    /// <returns>The same module builder for chained setup.</returns>
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
