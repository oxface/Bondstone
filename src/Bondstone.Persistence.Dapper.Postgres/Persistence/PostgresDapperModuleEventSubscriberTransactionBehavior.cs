using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.Persistence.Dapper.Postgres.Persistence;

internal sealed class PostgresDapperModuleEventSubscriberTransactionBehavior<TEvent>(
    IServiceProvider serviceProvider,
    IBondstoneModuleRegistry moduleRegistry)
    : IModuleEventSubscriberSystemPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly PostgresDapperModuleTransactionRunner _transactionRunner = new(
        serviceProvider,
        moduleRegistry);

    public int Order => ModuleEventSubscriberSystemPipelineOrder.Transaction;

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
            context.ModuleName,
            nextCt => next(nextCt),
            ct);
    }
}
