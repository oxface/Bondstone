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

        services.TryAddSingleton(_ => NpgsqlDataSource.Create(connectionString));
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
