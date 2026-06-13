using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests;

internal sealed class EntityFrameworkCoreTestDbContext(DbContextOptions<EntityFrameworkCoreTestDbContext> options)
    : DbContext(options)
{
    public static EntityFrameworkCoreTestDbContext Create()
    {
        var options = new DbContextOptionsBuilder<EntityFrameworkCoreTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new EntityFrameworkCoreTestDbContext(options);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBondstonePersistence();
    }
}
