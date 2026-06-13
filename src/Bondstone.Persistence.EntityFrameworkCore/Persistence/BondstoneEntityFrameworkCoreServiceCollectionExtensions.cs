using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

public static class BondstoneEntityFrameworkCoreServiceCollectionExtensions
{
    public static IServiceCollection AddBondstoneEntityFrameworkCorePersistence<TDbContext>(
        this IServiceCollection services)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IDurableOutboxWriter>(serviceProvider =>
            new EntityFrameworkCoreDurableOutboxWriter<TDbContext>(
                serviceProvider.GetRequiredService<TDbContext>(),
                serviceProvider.GetService<TimeProvider>()));
        services.TryAddScoped<IDurableInboxStore, EntityFrameworkCoreDurableInboxStore<TDbContext>>();
        services.TryAddScoped<IDurableOperationStateStore, EntityFrameworkCoreDurableOperationStateStore<TDbContext>>();
        services.TryAddScoped<
            IEntityFrameworkCorePersistenceScope,
            EntityFrameworkCorePersistenceScope<TDbContext>>();

        return services;
    }
}
