using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.EntityFrameworkCore.Persistence;

internal sealed class EntityFrameworkCoreModuleEventSubscriberTransactionBehavior<TEvent>(
    IServiceProvider serviceProvider,
    EntityFrameworkCoreModuleRuntimeRegistry moduleRuntimeRegistry)
    : IModuleEventSubscriberSystemPipelineBehavior<TEvent>
    where TEvent : IIntegrationEvent
{
    private readonly EntityFrameworkCoreModuleTransactionRunner _transactionRunner = new(
        serviceProvider,
        moduleRuntimeRegistry);

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
