using Bondstone.Configuration;
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
}
