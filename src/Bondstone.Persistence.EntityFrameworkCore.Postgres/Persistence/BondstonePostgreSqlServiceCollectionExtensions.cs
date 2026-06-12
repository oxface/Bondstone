using Bondstone.Configuration;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;

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
        services.TryAddScoped<IDurableInboxRegistrar>(serviceProvider =>
            new PostgreSqlDurableInboxRegistrar<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                schema));
        services.TryAddScoped<IDurableInboxHandlerExecutor>(serviceProvider =>
            new DurableInboxHandlerExecutor(
                serviceProvider.GetRequiredService<IDurableInboxRegistrar>(),
                serviceProvider.GetRequiredService<IDurableInboxStore>(),
                serviceProvider.GetService<TimeProvider>()));
        services.TryAddScoped<IDurableOutboxClaimer>(serviceProvider =>
            new PostgreSqlDurableOutboxClaimer<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>(),
                schema));
        services.TryAddScoped<IDurableOutboxLeaseRenewer>(serviceProvider =>
            new PostgreSqlDurableOutboxLeaseRenewer<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>(),
                schema));
        services.TryAddScoped<IDurableOutboxDispatchRecorder>(serviceProvider =>
            new PostgreSqlDurableOutboxDispatchRecorder<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                schema));

        return services;
    }

    public static IServiceCollection AddBondstonePostgreSqlModulePersistence<TDbContext>(
        this IServiceCollection services,
        string moduleName,
        string? schema = null)
        where TDbContext : DbContext
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
            serviceProvider => new EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>())));
        registry.AddInboxHandlerExecutor(new DurableModuleInboxHandlerExecutorRegistration(
            moduleName,
            serviceProvider => new PostgreSqlModuleDurableInboxHandlerExecutor<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>(),
                schema)));
        registry.AddOperationStateStore(new DurableModuleOperationStateStoreRegistration(
            moduleName,
            serviceProvider => new EntityFrameworkCoreModuleDurableOperationStateStore<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>())));
        registry.AddOutboxDispatcher(new DurableModuleOutboxDispatcherRegistration(
            moduleName,
            serviceProvider => new PostgreSqlModuleDurableOutboxDispatcher<TDbContext>(
                    moduleName,
                    serviceProvider.GetRequiredService<TDbContext>(),
                    serviceProvider.GetRequiredService<IDurableOutboxTransport>(),
                    serviceProvider.GetRequiredService<IDurableOutboxFailurePolicy>(),
                    serviceProvider.GetService<TimeProvider>(),
                    schema)));

        return services;
    }

}
