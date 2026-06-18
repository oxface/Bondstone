using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;

namespace Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;

internal sealed class EntityFrameworkCoreDurableIncomingInboxIngestionPersistenceScope(
    IEntityFrameworkCorePersistenceScope persistenceScope)
    : IDurableIncomingInboxIngestionPersistenceScope
{
    private readonly IEntityFrameworkCorePersistenceScope _persistenceScope =
        persistenceScope ?? throw new ArgumentNullException(nameof(persistenceScope));

    public ValueTask<TResult> ExecuteAsync<TResult>(
        Func<IDurableIncomingInboxIngestionPersistenceScope, CancellationToken, ValueTask<TResult>> operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return _persistenceScope.ExecuteAsync(
            async (_, innerCt) => await operation(this, innerCt),
            ct);
    }

    public ValueTask SaveChangesAsync(CancellationToken ct = default)
    {
        return _persistenceScope.SaveChangesAsync(ct);
    }
}
