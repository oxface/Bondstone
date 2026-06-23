using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests;

public sealed class PostgreSqlIncomingInboxTestDbContext(
    DbContextOptions<PostgreSqlIncomingInboxTestDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBondstoneIncomingInbox();
    }
}
