using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests;

internal sealed class MissingIncomingInboxMappingDbContext(DbContextOptions<MissingIncomingInboxMappingDbContext> options)
    : DbContext(options)
{
    public static MissingIncomingInboxMappingDbContext Create()
    {
        var options = new DbContextOptionsBuilder<MissingIncomingInboxMappingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new MissingIncomingInboxMappingDbContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBondstonePersistence();
    }
}
