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
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteAsync(
            async (scope, ct) =>
            {
                await operation(scope, ct);
                return true;
            },
            ct);
    }

    public async ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IEntityFrameworkCorePersistenceScope, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_context.Database.CurrentTransaction is not null)
        {
            return await operation(this, ct);
        }

        await using IDbContextTransaction transaction =
            await _context.Database.BeginTransactionAsync(ct);

        try
        {
            TResult result = await operation(this, ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async ValueTask SaveChangesAsync(CancellationToken ct = default)
    {
        await _context.SaveChangesAsync(ct);
    }
}
