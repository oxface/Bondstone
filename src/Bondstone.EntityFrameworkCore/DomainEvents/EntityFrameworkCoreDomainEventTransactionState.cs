using Bondstone.DomainEvents;
using Bondstone.EntityFrameworkCore.Persistence;

namespace Bondstone.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventTransactionState
    : IEntityFrameworkCoreModuleTransactionCompletion
{
    private readonly List<IDomainEventSource> _sources = [];

    public void AddCollectedSources(IEnumerable<IDomainEventSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        foreach (IDomainEventSource source in sources)
        {
            if (!_sources.Contains(source, ReferenceEqualityComparer.Instance))
            {
                _sources.Add(source);
            }
        }
    }

    public void ClearCollectedSources()
    {
        foreach (IDomainEventSource source in _sources)
        {
            source.ClearPendingDomainEvents();
        }

        _sources.Clear();
    }

    public ValueTask OnCommittedAsync(CancellationToken ct)
    {
        ClearCollectedSources();
        return ValueTask.CompletedTask;
    }
}
