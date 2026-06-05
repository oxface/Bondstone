using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.EntityFrameworkCore.Persistence;

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
        services.TryAddScoped<IDurableOperationReader>(serviceProvider =>
            serviceProvider.GetRequiredService<IDurableOperationStateStore>());

        return services;
    }
}
