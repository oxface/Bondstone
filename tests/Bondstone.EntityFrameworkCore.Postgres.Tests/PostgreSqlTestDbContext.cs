using Bondstone.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgreSqlTestDbContext(
    DbContextOptions<PostgreSqlTestDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBondstonePersistence();
    }
}
