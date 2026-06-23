using Bondstone.Persistence.EntityFrameworkCore.DomainEvents;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

/// <summary>
/// Adds Bondstone module-local domain event record mappings to EF Core models.
/// </summary>
public static class BondstoneDomainEventModelBuilderExtensions
{
    /// <summary>
    /// Applies the Bondstone domain event record mapping to the model.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    /// <param name="schema">The optional database schema for the domain event record table.</param>
    /// <returns>The same model builder for chained EF Core configuration.</returns>
    public static ModelBuilder ApplyBondstoneDomainEvents(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new DomainEventRecordEntityConfiguration(schema));

        return modelBuilder;
    }
}
