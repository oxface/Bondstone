using Bondstone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Samples.ModularMonolith.Ordering;

public sealed class OrderingDbContext(
    DbContextOptions<OrderingDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders", OrderingModule.Name);
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Sku).IsRequired();
        });

        modelBuilder.ApplyBondstonePersistence(OrderingModule.Name);
    }
}
