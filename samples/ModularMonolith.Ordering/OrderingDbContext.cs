using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Samples.ModularMonolith.Ordering;

public sealed class OrderingDbContext(
    DbContextOptions<OrderingDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderInventoryReservation> InventoryReservations =>
        Set<OrderInventoryReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders", OrderingModule.ModuleName);
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Sku).IsRequired();
        });

        modelBuilder.Entity<OrderInventoryReservation>(entity =>
        {
            entity.ToTable("inventory_reservations", OrderingModule.ModuleName);
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.OrderId).IsRequired();
            entity.Property(reservation => reservation.Sku).IsRequired();
        });

        modelBuilder.ApplyBondstonePersistence(OrderingModule.ModuleName);
    }
}
