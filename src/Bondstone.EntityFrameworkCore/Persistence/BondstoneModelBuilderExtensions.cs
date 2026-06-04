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

        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration(schema));
        modelBuilder.ApplyConfiguration(new InboxMessageEntityConfiguration(schema));
        modelBuilder.ApplyConfiguration(new OperationStateEntityConfiguration(schema));

        return modelBuilder;
    }
}
