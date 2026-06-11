using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.EntityFrameworkCore.Persistence;

internal sealed class EntityFrameworkCoreModuleTransactionBehavior<TCommand>(
    IServiceProvider serviceProvider,
    EntityFrameworkCoreModuleRuntimeRegistry moduleRuntimeRegistry)
    : IModuleCommandSystemPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly EntityFrameworkCoreModuleTransactionRunner _transactionRunner = new(
        serviceProvider,
        moduleRuntimeRegistry);

    public int Order => ModuleCommandSystemPipelineOrder.Transaction;

    public async ValueTask HandleAsync(
        TCommand command,
        ModuleCommandExecutionContext context,
        ModuleCommandPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        await _transactionRunner.ExecuteAsync(
            context,
            nextCt => next(nextCt),
            ct);
    }
}
