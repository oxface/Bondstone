using Bondstone.EntityFrameworkCore.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Bondstone.EntityFrameworkCore.DomainEvents;

internal static class EntityFrameworkCoreDomainEventServiceCollectionExtensions
{
    public static void TryAddEntityFrameworkCoreDomainEventTransactionState(
        this IServiceCollection services)
    {
        services.TryAddScoped<EntityFrameworkCoreDomainEventTransactionState>();

        services.AddScoped<IEntityFrameworkCoreModuleTransactionCompletion>(serviceProvider =>
            serviceProvider.GetRequiredService<EntityFrameworkCoreDomainEventTransactionState>());
    }
}
