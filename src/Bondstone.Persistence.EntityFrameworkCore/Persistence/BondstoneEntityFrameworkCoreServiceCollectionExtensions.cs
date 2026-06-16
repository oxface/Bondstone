using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

/// <summary>
/// Registers provider-neutral EF Core durable persistence services.
/// </summary>
public static class BondstoneEntityFrameworkCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers EF Core-backed durable outbox, inbox, operation-state, and persistence-scope services.
    /// </summary>
    /// <typeparam name="TDbContext">The DbContext type that contains Bondstone durable mappings.</typeparam>
    /// <param name="services">The service collection that receives the persistence registrations.</param>
    /// <returns>The same service collection for chained setup.</returns>
    public static IServiceCollection AddBondstoneEntityFrameworkCorePersistence<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IDurableOutboxWriter>(serviceProvider =>
            new EntityFrameworkCoreDurableOutboxWriter<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>()));
        services.TryAddScoped<IDurableOutboxInspectionStore>(serviceProvider =>
            new EntityFrameworkCoreDurableOutboxInspectionStore<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>()));
        services.TryAddScoped<IDurableInboxStore, EntityFrameworkCoreDurableInboxStore<TDbContext>>();
        services.TryAddScoped<IDurableInboxInspectionStore>(serviceProvider =>
            new EntityFrameworkCoreDurableInboxInspectionStore<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>()));
        services.TryAddScoped<IDurableOperationStateStore, EntityFrameworkCoreDurableOperationStateStore<TDbContext>>();
        services.TryAddScoped<
            IEntityFrameworkCorePersistenceScope,
            EntityFrameworkCorePersistenceScope<TDbContext>>();

        return services;
    }
}
