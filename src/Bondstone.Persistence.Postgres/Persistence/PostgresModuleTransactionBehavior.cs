using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.Persistence.Postgres.Persistence;

internal sealed class PostgresModuleTransactionBehavior<TCommand>(
    IServiceProvider serviceProvider,
    PostgresModuleRuntimeRegistry moduleRuntimeRegistry)
    : IModuleCommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly PostgresModuleTransactionRunner _transactionRunner = new(
        serviceProvider,
        moduleRuntimeRegistry);

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
            context.ModuleName,
            nextCt => next(nextCt),
            ct);
    }
}
