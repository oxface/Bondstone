using Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Outbox;

public sealed class PostgreSqlDurableOutboxDispatchRecorderTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkDispatchedAsync_WhenClaimedByIsBlank_Throws()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        var recorder = new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await recorder.MarkDispatchedAsync(
                Guid.NewGuid(),
                " ",
                DateTimeOffset.Parse("2026-06-04T00:00:00+00:00")));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ScheduleRetryAsync_WhenFailureReasonIsBlank_Throws()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        var recorder = new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await recorder.ScheduleRetryAsync(
                Guid.NewGuid(),
                "dispatcher-1",
                " ",
                DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"),
                DateTimeOffset.Parse("2026-06-04T00:01:00+00:00")));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ScheduleRetryAsync_WhenNextAttemptIsBeforeFailure_Throws()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        var recorder = new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await recorder.ScheduleRetryAsync(
                Guid.NewGuid(),
                "dispatcher-1",
                "transport unavailable",
                DateTimeOffset.Parse("2026-06-04T00:01:00+00:00"),
                DateTimeOffset.Parse("2026-06-04T00:00:59+00:00")));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task MarkTerminalFailedAsync_WhenFailedAtHasNonUtcOffset_Throws()
    {
        await using PostgreSqlTestDbContext context = CreateContext();
        var recorder = new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await recorder.MarkTerminalFailedAsync(
                Guid.NewGuid(),
                "dispatcher-1",
                "poison message",
                DateTimeOffset.Parse("2026-06-04T00:00:00+02:00")));
    }

    private static PostgreSqlTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlTestDbContext>()
            .UseNpgsql("Host=localhost;Database=bondstone")
            .Options;

        return new PostgreSqlTestDbContext(options);
    }
}
