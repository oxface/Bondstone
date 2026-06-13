using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.Persistence;

public sealed class EntityFrameworkCorePersistenceScopeTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenDbContextIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EntityFrameworkCorePersistenceScope<EntityFrameworkCoreTestDbContext>(null!));
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ExecuteAsync_WhenOperationIsNull_Throws()
    {
        using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var scope = new EntityFrameworkCorePersistenceScope<EntityFrameworkCoreTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await scope.ExecuteAsync(
                (Func<IEntityFrameworkCorePersistenceScope, CancellationToken, ValueTask>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await scope.ExecuteAsync<bool>(
                (Func<IEntityFrameworkCorePersistenceScope, CancellationToken, ValueTask<bool>>)null!));
    }
}
