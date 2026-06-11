namespace Bondstone.EntityFrameworkCore.Persistence;

internal interface IEntityFrameworkCoreModuleTransactionCompletion
{
    ValueTask OnCommittedAsync(
        string moduleName,
        CancellationToken ct);
}
