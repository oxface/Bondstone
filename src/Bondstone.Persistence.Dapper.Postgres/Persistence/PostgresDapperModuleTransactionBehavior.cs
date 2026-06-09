using Bondstone.Messaging;
using Bondstone.Modules;

namespace Bondstone.Persistence.Dapper.Postgres.Persistence;

internal sealed class PostgresDapperModuleTransactionBehavior<TCommand>(
    IServiceProvider serviceProvider,
    IBondstoneModuleRegistry moduleRegistry)
    : IModuleCommandSystemPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly PostgresDapperModuleTransactionRunner _transactionRunner = new(
        serviceProvider,
        moduleRegistry);

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
            context.ModuleName,
            nextCt => next(nextCt),
            ct);
    }
}
