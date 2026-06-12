using Bondstone.Configuration;
using Bondstone.Persistence.Postgres.Inbox;
using Bondstone.Persistence.Postgres.Operations;
using Bondstone.Persistence.Postgres.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Bondstone.Persistence.Postgres.Persistence;

public static class BondstonePostgresServiceCollectionExtensions
{
    public static IServiceCollection AddBondstonePostgresPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        string normalizedConnectionString = connectionString.Trim();
        PostgresPersistenceOptions? existingOptions = services
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(PostgresPersistenceOptions))
            .Select(static descriptor =>
                descriptor.ImplementationInstance as PostgresPersistenceOptions)
            .SingleOrDefault(static options => options is not null);

        if (existingOptions is not null)
        {
            if (!StringComparer.Ordinal.Equals(
                existingOptions.ConnectionString,
                normalizedConnectionString))
            {
                throw new InvalidOperationException(
                    "Bondstone PostgreSQL persistence currently supports one Npgsql data source per service provider. Use separate service providers for different connection strings, or keep modules on separate schemas in the same database.");
            }
        }
        else
        {
            services.AddSingleton(new PostgresPersistenceOptions(
                normalizedConnectionString));
        }

        services.TryAddSingleton(serviceProvider =>
            NpgsqlDataSource.Create(serviceProvider
                .GetRequiredService<PostgresPersistenceOptions>()
                .ConnectionString));
        services.TryAddScoped<IPostgresModuleSession, PostgresModuleSession>();
        services.TryAddScoped<PostgresModuleTransactionGuard>();

        return services;
    }

    public static IServiceCollection AddBondstonePostgresModulePersistence(
        this IServiceCollection services,
        string moduleName,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new ArgumentException("Module name is required.", nameof(moduleName));
        }

        services.UseDurableModuleOutboxDispatchAggregator();
        DurableModulePersistenceRegistrationRegistry registry =
            services.GetOrAddDurableModulePersistenceRegistrationRegistry();
        registry.AddOutboxWriter(new DurableModuleOutboxWriterRegistration(
            moduleName,
            serviceProvider => new PostgresModuleDurableOutboxWriter(
                moduleName,
                serviceProvider.GetRequiredService<IPostgresModuleSession>(),
                serviceProvider.GetService<TimeProvider>(),
                schema)));
        registry.AddInboxHandlerExecutor(new DurableModuleInboxHandlerExecutorRegistration(
            moduleName,
            serviceProvider => new PostgresModuleDurableInboxHandlerExecutor(
                moduleName,
                serviceProvider.GetRequiredService<IPostgresModuleSession>(),
                serviceProvider.GetService<TimeProvider>(),
                schema)));
        registry.AddOperationStateStore(new DurableModuleOperationStateStoreRegistration(
            moduleName,
            serviceProvider => new PostgresModuleDurableOperationStateStore(
                moduleName,
                serviceProvider.GetRequiredService<IPostgresModuleSession>(),
                schema)));
        registry.AddOutboxDispatcher(new DurableModuleOutboxDispatcherRegistration(
            moduleName,
            serviceProvider => new PostgresModuleDurableOutboxDispatcher(
                    moduleName,
                    serviceProvider.GetRequiredService<NpgsqlDataSource>(),
                    serviceProvider.GetRequiredService<IDurableOutboxTransport>(),
                    serviceProvider.GetRequiredService<IDurableOutboxFailurePolicy>(),
                    serviceProvider.GetService<TimeProvider>(),
                    schema)));

        return services;
    }

}
