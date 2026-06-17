using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Persistence;

/// <summary>
/// Adds Bondstone durable persistence mappings to EF Core models.
/// </summary>
public static class BondstoneModelBuilderExtensions
{
    /// <summary>
    /// Applies Bondstone outbox, inbox, and operation-state mappings to the model.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    /// <param name="schema">The optional database schema for Bondstone durable tables.</param>
    /// <returns>The same model builder for chained EF Core configuration.</returns>
    public static ModelBuilder ApplyBondstonePersistence(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyBondstoneOutbox(schema);
        modelBuilder.ApplyBondstoneInbox(schema);
        modelBuilder.ApplyBondstoneOperationState(schema);

        return modelBuilder;
    }

    /// <summary>
    /// Applies the Bondstone durable outbox mapping to the model.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    /// <param name="schema">The optional database schema for the outbox table.</param>
    /// <returns>The same model builder for chained EF Core configuration.</returns>
    public static ModelBuilder ApplyBondstoneOutbox(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration(schema));

        return modelBuilder;
    }

    /// <summary>
    /// Applies the Bondstone durable inbox mapping to the model.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    /// <param name="schema">The optional database schema for the inbox table.</param>
    /// <returns>The same model builder for chained EF Core configuration.</returns>
    public static ModelBuilder ApplyBondstoneInbox(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration(schema));

        return modelBuilder;
    }

    /// <summary>
    /// Applies the Bondstone durable operation-state mapping to the model.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    /// <param name="schema">The optional database schema for the operation-state table.</param>
    /// <returns>The same model builder for chained EF Core configuration.</returns>
    public static ModelBuilder ApplyBondstoneOperationState(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new OperationStateEntityConfiguration(schema));

        return modelBuilder;
    }

    /// <summary>
    /// Applies the optional Bondstone durable incoming inbox mapping to the model.
    /// </summary>
    /// <param name="modelBuilder">The EF Core model builder.</param>
    /// <param name="schema">The optional database schema for the incoming inbox table.</param>
    /// <returns>The same model builder for chained EF Core configuration.</returns>
    public static ModelBuilder ApplyBondstoneIncomingInbox(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new IncomingInboxMessageEntityConfiguration(schema));

        return modelBuilder;
    }
}
