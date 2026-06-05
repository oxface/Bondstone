namespace Bondstone.EntityFrameworkCore.Persistence;

public interface IEntityFrameworkCorePersistenceScope
{
    ValueTask ExecuteAsync(
        Func<IEntityFrameworkCorePersistenceScope, CancellationToken, ValueTask> operation,
        CancellationToken ct = default);

    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IEntityFrameworkCorePersistenceScope, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken ct = default);

    ValueTask SaveChangesAsync(CancellationToken ct = default);
}
