using Bondstone.DomainEvents;

namespace Bondstone.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventTransactionState
{
    private readonly List<CollectedSource> _sources = [];

    public void AddCollectedSources(
        string moduleName,
        IEnumerable<IDomainEventSource> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);
        ArgumentNullException.ThrowIfNull(sources);

        string normalizedModuleName = moduleName.Trim();
        foreach (IDomainEventSource source in sources)
        {
            if (!_sources.Any(existing =>
                StringComparer.Ordinal.Equals(existing.ModuleName, normalizedModuleName)
                && ReferenceEquals(existing.Source, source)))
            {
                _sources.Add(new CollectedSource(normalizedModuleName, source));
            }
        }
    }

    public void ClearCollectedSources(string moduleName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        string normalizedModuleName = moduleName.Trim();
        CollectedSource[] sources = _sources
            .Where(source => StringComparer.Ordinal.Equals(source.ModuleName, normalizedModuleName))
            .ToArray();

        foreach (CollectedSource source in sources)
        {
            source.Source.ClearPendingDomainEvents();
        }

        _sources.RemoveAll(source => StringComparer.Ordinal.Equals(source.ModuleName, normalizedModuleName));
    }

    private sealed record CollectedSource(
        string ModuleName,
        IDomainEventSource Source);
}
