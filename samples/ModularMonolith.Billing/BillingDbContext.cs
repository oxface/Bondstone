using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Samples.ModularMonolith.Billing;

public sealed class BillingDbContext(
    DbContextOptions<BillingDbContext> options)
    : DbContext(options)
{
    public DbSet<BillingInvoice> Invoices => Set<BillingInvoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BillingInvoice>(entity =>
        {
            entity.ToTable("invoices", BillingModule.ModuleName);
            entity.HasKey(invoice => invoice.Id);
            entity.Property(invoice => invoice.OrderId).IsRequired();
            entity.Property(invoice => invoice.Sku).IsRequired();
        });

        modelBuilder.ApplyBondstonePersistence(BillingModule.ModuleName);
    }
}
