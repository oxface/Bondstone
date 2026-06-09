using Bondstone.Persistence.Dapper.Postgres.Inbox;
using Bondstone.Persistence.Dapper.Postgres.Operations;
using Bondstone.Persistence.Dapper.Postgres.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Bondstone.Persistence.Dapper.Postgres.Persistence;

public static class BondstonePostgresDapperServiceCollectionExtensions
{
    public static IServiceCollection AddBondstonePostgresDapperPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        string normalizedConnectionString = connectionString.Trim();
        PostgresDapperPersistenceOptions? existingOptions = services
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(PostgresDapperPersistenceOptions))
            .Select(static descriptor =>
                descriptor.ImplementationInstance as PostgresDapperPersistenceOptions)
            .SingleOrDefault(static options => options is not null);

        if (existingOptions is not null)
        {
            if (!StringComparer.Ordinal.Equals(
                existingOptions.ConnectionString,
                normalizedConnectionString))
            {
                throw new InvalidOperationException(
                    "Bondstone PostgreSQL Dapper persistence currently supports one Npgsql data source per service provider. Use separate service providers for different connection strings, or keep modules on separate schemas in the same database.");
            }
        }
        else
        {
            services.AddSingleton(new PostgresDapperPersistenceOptions(
                normalizedConnectionString));
        }

        services.TryAddSingleton(serviceProvider =>
            NpgsqlDataSource.Create(serviceProvider
                .GetRequiredService<PostgresDapperPersistenceOptions>()
                .ConnectionString));
        services.TryAddScoped<IPostgresDapperModuleSession, PostgresDapperModuleSession>();

        return services;
    }

    public static IServiceCollection AddBondstonePostgresDapperModulePersistence(
        this IServiceCollection services,
        string moduleName,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(moduleName))
        {
            throw new ArgumentException("Module name is required.", nameof(moduleName));
        }

        ReplaceDefaultDispatcherWithModuleAggregator(services);
        services.AddScoped<IDurableModuleOutboxWriter>(serviceProvider =>
            new PostgresDapperModuleDurableOutboxWriter(
                moduleName,
                serviceProvider.GetRequiredService<IPostgresDapperModuleSession>(),
                serviceProvider.GetService<TimeProvider>(),
                schema));
        services.AddScoped<IDurableModuleInboxHandlerExecutor>(serviceProvider =>
            new PostgresDapperModuleDurableInboxHandlerExecutor(
                moduleName,
                serviceProvider.GetRequiredService<IPostgresDapperModuleSession>(),
                serviceProvider.GetService<TimeProvider>(),
                schema));
        services.AddScoped<IDurableModuleOperationStateStore>(serviceProvider =>
            new PostgresDapperModuleDurableOperationStateStore(
                moduleName,
                serviceProvider.GetRequiredService<IPostgresDapperModuleSession>(),
                schema));
        services.AddScoped<IDurableModuleOutboxDispatcher>(serviceProvider =>
            new PostgresDapperModuleDurableOutboxDispatcher(
                moduleName,
                serviceProvider.GetRequiredService<NpgsqlDataSource>(),
                serviceProvider.GetRequiredService<IDurableOutboxTransport>(),
                serviceProvider.GetRequiredService<IDurableOutboxFailurePolicy>(),
                serviceProvider.GetService<TimeProvider>(),
                schema));

        return services;
    }

    private static void ReplaceDefaultDispatcherWithModuleAggregator(IServiceCollection services)
    {
        ServiceDescriptor[] defaultDispatcherDescriptors = services
            .Where(descriptor =>
                descriptor.ServiceType == typeof(IDurableOutboxDispatcher)
                && descriptor.ImplementationType == typeof(DurableOutboxDispatcher))
            .ToArray();

        foreach (ServiceDescriptor descriptor in defaultDispatcherDescriptors)
        {
            services.Remove(descriptor);
        }

        services.TryAddTransient<IDurableOutboxDispatcher, DurableModuleOutboxDispatchAggregator>();
    }
}
