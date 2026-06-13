using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgreSqlSchemaTestDbContext(
    DbContextOptions<PostgreSqlSchemaTestDbContext> options)
    : DbContext(options)
{
    public const string BondstoneSchema = "bondstone";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBondstonePersistence(BondstoneSchema);
    }
}
