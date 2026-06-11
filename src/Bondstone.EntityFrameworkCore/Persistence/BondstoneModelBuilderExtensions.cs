using Bondstone.EntityFrameworkCore.DomainEvents;
using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.EntityFrameworkCore.Persistence;

public static class BondstoneModelBuilderExtensions
{
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

    public static ModelBuilder ApplyBondstoneOutbox(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration(schema));

        return modelBuilder;
    }

    public static ModelBuilder ApplyBondstoneInbox(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration(schema));

        return modelBuilder;
    }

    public static ModelBuilder ApplyBondstoneOperationState(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new OperationStateEntityConfiguration(schema));

        return modelBuilder;
    }

    public static ModelBuilder ApplyBondstoneDomainEvents(
        this ModelBuilder modelBuilder,
        string? schema = null)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfiguration(new DomainEventRecordEntityConfiguration(schema));

        return modelBuilder;
    }
}
