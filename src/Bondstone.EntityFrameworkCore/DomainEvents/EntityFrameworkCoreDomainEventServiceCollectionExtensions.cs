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

        services.TryAddEnumerable(ServiceDescriptor.Scoped<
            IEntityFrameworkCoreModuleTransactionCompletion,
            EntityFrameworkCoreDomainEventTransactionCompletion>());
    }

    private sealed class EntityFrameworkCoreDomainEventTransactionCompletion(
        EntityFrameworkCoreDomainEventTransactionState transactionState)
        : IEntityFrameworkCoreModuleTransactionCompletion
    {
        private readonly EntityFrameworkCoreDomainEventTransactionState _transactionState =
            transactionState ?? throw new ArgumentNullException(nameof(transactionState));

        public ValueTask OnCommittedAsync(
            string moduleName,
            CancellationToken ct)
        {
            _transactionState.ClearCollectedSources(moduleName);
            return ValueTask.CompletedTask;
        }
    }
}
