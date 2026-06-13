using Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Outbox;

public sealed class PostgreSqlDurableOutboxClaimerTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ClaimAsync_WhenClaimedByIsBlank_Throws()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await claimer.ClaimAsync("   ", TimeSpan.FromSeconds(1)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ClaimAsync_WhenLeaseDurationIsNotPositive_Throws()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await claimer.ClaimAsync("dispatcher-1", TimeSpan.Zero));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ClaimAsync_WhenMaxCountIsNotPositive_Throws()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            async () => await claimer.ClaimAsync(
                "dispatcher-1",
                TimeSpan.FromSeconds(1),
                maxCount: 0));
    }

    private static PostgreSqlTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlTestDbContext>()
            .UseNpgsql("Host=localhost;Database=bondstone")
            .Options;

        return new PostgreSqlTestDbContext(options);
    }
}
