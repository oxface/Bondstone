using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Bondstone.EntityFrameworkCore.Postgres.Persistence;

public static class BondstonePostgreSqlServiceCollectionExtensions
{
    public static IServiceCollection AddBondstonePostgreSqlPersistence<TDbContext>(
        this IServiceCollection services,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null,
        string? schema = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        services.AddDbContext<TDbContext>(optionsBuilder =>
            optionsBuilder.UseNpgsql(connectionString, configureNpgsql));
        services.AddBondstoneEntityFrameworkCorePersistence<TDbContext>();
        services.TryAddScoped<IDurableOutboxClaimer>(serviceProvider =>
            new PostgreSqlDurableOutboxClaimer<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>(),
                schema));

        return services;
    }
}
