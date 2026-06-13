using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.Persistence.Postgres.Persistence;

internal sealed class PostgresModuleEventSubscriberTransactionBehavior<TEvent>(
    IServiceProvider serviceProvider,
    PostgresModuleRuntimeRegistry moduleRuntimeRegistry)
    : IModuleEventSubscriberPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly PostgresModuleTransactionRunner _transactionRunner = new(
        serviceProvider,
        moduleRuntimeRegistry);

    public async ValueTask HandleAsync(
        TEvent integrationEvent,
        ModuleEventSubscriberExecutionContext context,
        ModuleEventSubscriberPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _transactionRunner.ExecuteAsync(
            context,
            nextCt => next(nextCt),
            ct);
    }
}
