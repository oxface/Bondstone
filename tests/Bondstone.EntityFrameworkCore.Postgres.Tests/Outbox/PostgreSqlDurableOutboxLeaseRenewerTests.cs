using Bondstone.EntityFrameworkCore.Postgres.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Postgres.Tests.Outbox;

public sealed class PostgreSqlDurableOutboxLeaseRenewerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task RenewAsync_WhenClaimedByIsBlank_Throws()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        var renewer = new PostgreSqlDurableOutboxLeaseRenewer<PostgreSqlTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await renewer.RenewAsync(
                Guid.NewGuid(),
                "   ",
                TimeSpan.FromSeconds(1)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RenewAsync_WhenLeaseDurationIsNotPositive_Throws()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        var renewer = new PostgreSqlDurableOutboxLeaseRenewer<PostgreSqlTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await renewer.RenewAsync(
                Guid.NewGuid(),
                "dispatcher-1",
                TimeSpan.Zero));
    }

    private static PostgreSqlTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlTestDbContext>()
            .UseNpgsql("Host=localhost;Database=bondstone")
            .Options;

        return new PostgreSqlTestDbContext(options);
    }
}
