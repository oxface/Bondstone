using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class FulfillmentDbContext(
    DbContextOptions<FulfillmentDbContext> options)
    : DbContext(options)
{
    public DbSet<FulfillmentReservation> Reservations => Set<FulfillmentReservation>();

    public DbSet<FulfillmentOrderEvent> OrderEvents => Set<FulfillmentOrderEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FulfillmentReservation>(entity =>
        {
            entity.ToTable("reservations", FulfillmentModule.ModuleName);
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.OrderId).IsRequired();
            entity.Property(reservation => reservation.Sku).IsRequired();
        });

        modelBuilder.Entity<FulfillmentOrderEvent>(entity =>
        {
            entity.ToTable("order_events", FulfillmentModule.ModuleName);
            entity.HasKey(orderEvent => orderEvent.Id);
            entity.Property(orderEvent => orderEvent.OrderId).IsRequired();
            entity.Property(orderEvent => orderEvent.Sku).IsRequired();
        });

        modelBuilder.ApplyBondstonePersistence(FulfillmentModule.ModuleName);
    }
}
