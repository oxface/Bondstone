using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bondstone.EntityFrameworkCore.Persistence;

public sealed class EntityFrameworkCorePersistenceScope<TDbContext>(TDbContext context)
    : IEntityFrameworkCorePersistenceScope
    where TDbContext : DbContext
{
    private readonly TDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    public async ValueTask ExecuteAsync(
        Func<IEntityFrameworkCorePersistenceScope, CancellationToken, ValueTask> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteAsync(
            async (scope, ct) =>
            {
                await operation(scope, ct);
                return true;
            },
            cancellationToken);
    }

    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IEntityFrameworkCorePersistenceScope, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_context.Database.CurrentTransaction is not null)
        {
            return await operation(this, cancellationToken);
        }

        await using IDbContextTransaction transaction =
            await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            TResult result = await operation(this, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async ValueTask SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
