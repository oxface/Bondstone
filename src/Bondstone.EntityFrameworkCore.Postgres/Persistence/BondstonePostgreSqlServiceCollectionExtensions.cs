using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Inbox;
using Bondstone.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
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

        ReplaceDefaultDispatcherWithModuleAggregator(services);
        services.AddSingleton(new DurableModuleOutboxWriterRegistration(
            moduleName,
            serviceProvider => new EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>())));
        services.AddSingleton(new DurableModuleInboxHandlerExecutorRegistration(
            moduleName,
            serviceProvider => new PostgreSqlModuleDurableInboxHandlerExecutor<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>(),
                schema)));
        services.AddSingleton(new DurableModuleOperationStateStoreRegistration(
            moduleName,
            serviceProvider => new EntityFrameworkCoreModuleDurableOperationStateStore<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>())));
        services.AddScoped<IDurableModuleOutboxDispatcher>(serviceProvider =>
            new PostgreSqlModuleDurableOutboxDispatcher<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>(),
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
