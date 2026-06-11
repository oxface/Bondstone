using Bondstone.Capabilities.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.DomainEvents;

internal sealed class EntityFrameworkCoreDomainEventSourceFeature(DbContext dbContext)
    : IDomainEventSourceFeature
{
    private readonly DbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public IReadOnlyCollection<IDomainEventSource> GetPendingDomainEventSources()
    {
        return EntityFrameworkCoreDomainEventCollector.GetPendingDomainEventSources(_dbContext);
    }
}
