namespace Bondstone.EntityFrameworkCore.Persistence;

public interface IEntityFrameworkCorePersistenceScope
{
    ValueTask ExecuteAsync(
        Func<IEntityFrameworkCorePersistenceScope, CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default);

    ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IEntityFrameworkCorePersistenceScope, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default);

    ValueTask SaveChangesAsync(CancellationToken cancellationToken = default);
}
