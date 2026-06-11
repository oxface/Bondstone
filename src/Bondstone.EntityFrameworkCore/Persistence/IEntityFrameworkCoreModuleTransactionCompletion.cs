namespace Bondstone.EntityFrameworkCore.Persistence;

internal interface IEntityFrameworkCoreModuleTransactionCompletion
{
    ValueTask OnCommittedAsync(CancellationToken ct);
}
