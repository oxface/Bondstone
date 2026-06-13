using Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.Persistence;

public static class BondstoneDomainEventModelBuilderExtensions
{
    public static ModelBuilder ApplyBondstoneDomainEvents(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new DomainEventRecordEntityConfiguration(schema));

        return modelBuilder;
    }
}
