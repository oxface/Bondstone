using Bondstone.Configuration;
using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.IncomingInbox;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence;
using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;

/// <summary>
/// Registers PostgreSQL-backed EF Core durable persistence services.
/// </summary>
public static class BondstonePostgreSqlServiceCollectionExtensions
{
    /// <summary>
    /// Registers PostgreSQL DbContext and durable outbox, inbox, and operation-state services.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type that contains Bondstone durable mappings.</typeparam>
    /// <param name="services">The service collection that receives the persistence registrations.</param>
    /// <param name="connectionString">The PostgreSQL connection string used by the DbContext.</param>
    /// <param name="configureNpgsql">Optional Npgsql provider configuration.</param>
    /// <param name="schema">The optional schema for Bondstone durable tables.</param>
    /// <returns>The same service collection for chained setup.</returns>
    public static IServiceCollection AddBondstonePostgreSqlPersistence<TDbContext>(
        this IServiceCollection services,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null,
        string? schema = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        AddPostgreSqlDbContext<TDbContext>(
            services,
            connectionString,
            configureNpgsql);
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
        services.TryAddScoped<IDurableIncomingInboxClaimer>(serviceProvider =>
            new PostgreSqlDurableIncomingInboxClaimer<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>(),
                schema));
        services.TryAddScoped<IDurableIncomingInboxLeaseRenewer>(serviceProvider =>
            new PostgreSqlDurableIncomingInboxLeaseRenewer<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>(),
                schema));
        services.TryAddScoped<IDurableIncomingInboxOutcomeRecorder>(serviceProvider =>
            new PostgreSqlDurableIncomingInboxOutcomeRecorder<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                schema));

        return services;
    }

    /// <summary>
    /// Registers PostgreSQL DbContext infrastructure for module-owned EF Core durable persistence.
    /// </summary>
    /// <typeparam name="TDbContext">The module DbContext type.</typeparam>
    /// <param name="services">The service collection that receives the infrastructure registrations.</param>
    /// <param name="connectionString">The PostgreSQL connection string used by the DbContext.</param>
    /// <param name="configureNpgsql">Optional Npgsql provider configuration.</param>
    /// <returns>The same service collection for chained setup.</returns>
    public static IServiceCollection AddBondstonePostgreSqlModuleInfrastructure<TDbContext>(
        this IServiceCollection services,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql = null)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        AddPostgreSqlDbContext<TDbContext>(
            services,
            connectionString,
            configureNpgsql);

        return services;
    }

    /// <summary>
    /// Registers module-scoped PostgreSQL EF Core durable outbox, inbox, operation-state, and dispatcher services.
    /// </summary>
    /// <typeparam name="TDbContext">The module DbContext type.</typeparam>
    /// <param name="services">The service collection that receives the module persistence registrations.</param>
    /// <param name="moduleName">The module name that owns the persistence registrations.</param>
    /// <param name="schema">The optional schema for Bondstone durable tables.</param>
    /// <returns>The same service collection for chained setup.</returns>
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
        services.UseDurableModuleIncomingInboxDispatcherAggregator();
        DurableModulePersistenceRegistrationRegistry registry =
            services.GetOrAddDurableModulePersistenceRegistrationRegistry();
        registry.AddOutboxWriter(new DurableModuleOutboxWriterRegistration(
            moduleName,
            serviceProvider => new EntityFrameworkCoreModuleDurableOutboxWriter<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>())));
        registry.AddOutboxInspectionStore(new DurableModuleOutboxInspectionStoreRegistration(
            moduleName,
            serviceProvider => new EntityFrameworkCoreDurableOutboxInspectionStore<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>())));
        registry.AddInboxHandlerExecutor(new DurableModuleInboxHandlerExecutorRegistration(
            moduleName,
            serviceProvider => new PostgreSqlModuleDurableInboxHandlerExecutor<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>(),
                schema)));
        registry.AddInboxInspectionStore(new DurableModuleInboxInspectionStoreRegistration(
            moduleName,
            serviceProvider => new EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>())));
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
                    serviceProvider.GetRequiredService<IDurableEnvelopeDispatcher>(),
                    serviceProvider.GetRequiredService<IDurableOutboxFailurePolicy>(),
                    serviceProvider.GetService<TimeProvider>(),
                    schema)));
        registry.AddIncomingInboxDispatcher(new DurableModuleIncomingInboxDispatcherRegistration(
            moduleName,
            serviceProvider => new PostgreSqlModuleDurableIncomingInboxDispatcher<TDbContext>(
                moduleName,
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetRequiredService<IModuleCommandReceivePipeline>(),
                serviceProvider.GetRequiredService<IModuleEventReceivePipeline>(),
                serviceProvider.GetRequiredService<IDurableIncomingInboxFailurePolicy>(),
                serviceProvider.GetService<TimeProvider>(),
                schema)));

        return services;
    }

    private static void AddPostgreSqlDbContext<TDbContext>(
        IServiceCollection services,
        string connectionString,
        Action<NpgsqlDbContextOptionsBuilder>? configureNpgsql)
        where TDbContext : DbContext
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        services.AddDbContext<TDbContext>(optionsBuilder =>
            optionsBuilder.UseNpgsql(connectionString, configureNpgsql));
    }
}
