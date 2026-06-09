using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Samples.ModularMonolith.Fulfillment;

public sealed class FulfillmentDbContext(
    DbContextOptions<FulfillmentDbContext> options)
    : DbContext(options)
{
    public DbSet<FulfillmentReservation> Reservations => Set<FulfillmentReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FulfillmentReservation>(entity =>
        {
            entity.ToTable("reservations", FulfillmentModule.Name);
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.OrderId).IsRequired();
            entity.Property(reservation => reservation.Sku).IsRequired();
        });

        modelBuilder.ApplyBondstonePersistence(FulfillmentModule.Name);
    }
}
