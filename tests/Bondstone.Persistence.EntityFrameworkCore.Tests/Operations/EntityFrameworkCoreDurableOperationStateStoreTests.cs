using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Messaging;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.Operations;

public sealed class EntityFrameworkCoreDurableOperationStateStoreTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task GetStateAsync_WhenOperationDoesNotExist_ReturnsNull()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableOperationStateStore<EntityFrameworkCoreTestDbContext>(context);

        DurableOperationState? state = await store.GetStateAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

        Assert.Null(state);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SaveAsync_WhenOperationIsNew_StagesOperationState()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableOperationStateStore<EntityFrameworkCoreTestDbContext>(context);
        var state = new DurableOperationState(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DurableOperationStatus.Pending,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));

        await store.SaveAsync(state);

        Assert.Single(context.ChangeTracker.Entries<OperationStateEntity>());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SaveAsync_WhenOperationExists_UpdatesOperationState()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableOperationStateStore<EntityFrameworkCoreTestDbContext>(context);
        Guid durableOperationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var pending = new DurableOperationState(
            durableOperationId,
            DurableOperationStatus.Pending,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
        var completed = new DurableOperationState(
            durableOperationId,
            DurableOperationStatus.Completed,
            DateTimeOffset.Parse("2026-06-04T00:01:00+00:00"),
            """ {"ok":true} """,
            diagnosticContext: new DurableOperationDiagnosticContext(
                "fulfillment",
                "fulfillment.order.reserve.v1",
                "receive.fulfillment.order.reserve.v1"));

        await store.SaveAsync(pending);
        await context.SaveChangesAsync();
        await store.SaveAsync(completed);
        await context.SaveChangesAsync();

        DurableOperationState? mapped = await store.GetStateAsync(durableOperationId);
        Assert.Equal(completed, mapped);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task FindExpirationCandidatesAsync_WhenOperationsAreStale_ReturnsPendingAndRunningCandidates()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableOperationStateStore<EntityFrameworkCoreTestDbContext>(context);
        var pending = new DurableOperationState(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DurableOperationStatus.Pending,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
        var running = new DurableOperationState(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DurableOperationStatus.Running,
            DateTimeOffset.Parse("2026-06-04T00:01:00+00:00"));
        var freshPending = new DurableOperationState(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            DurableOperationStatus.Pending,
            DateTimeOffset.Parse("2026-06-04T00:05:00+00:00"));
        var completed = new DurableOperationState(
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            DurableOperationStatus.Completed,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));

        await store.SaveAsync(pending);
        await store.SaveAsync(running);
        await store.SaveAsync(freshPending);
        await store.SaveAsync(completed);
        await context.SaveChangesAsync();

        IReadOnlyList<DurableOperationState> candidates =
            await store.FindExpirationCandidatesAsync(
                DateTimeOffset.Parse("2026-06-04T00:01:00+00:00"),
                maxCount: 10);

        Assert.Equal([pending, running], candidates);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task FindExpirationCandidatesAsync_WhenMaxCountIsLowerThanCandidateCount_LimitsResults()
    {
        await using EntityFrameworkCoreTestDbContext context = EntityFrameworkCoreTestDbContext.Create();
        var store = new EntityFrameworkCoreDurableOperationStateStore<EntityFrameworkCoreTestDbContext>(context);
        var first = new DurableOperationState(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DurableOperationStatus.Pending,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
        var second = new DurableOperationState(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            DurableOperationStatus.Pending,
            DateTimeOffset.Parse("2026-06-04T00:01:00+00:00"));

        await store.SaveAsync(first);
        await store.SaveAsync(second);
        await context.SaveChangesAsync();

        IReadOnlyList<DurableOperationState> candidates =
            await store.FindExpirationCandidatesAsync(
                DateTimeOffset.Parse("2026-06-04T00:05:00+00:00"),
                maxCount: 1);

        Assert.Equal([first], candidates);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task SaveAsync_WhenOperationStateMappingIsMissing_ThrowsClearError()
    {
        await using var context = MissingOperationStateMappingDbContext.Create();
        var store =
            new EntityFrameworkCoreDurableOperationStateStore<MissingOperationStateMappingDbContext>(
                context);
        var state = new DurableOperationState(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DurableOperationStatus.Pending,
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await store.SaveAsync(state));

        Assert.Contains("operation-state mapping", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ApplyBondstoneOperationState()", exception.Message, StringComparison.Ordinal);
    }

    private sealed class MissingOperationStateMappingDbContext(
        DbContextOptions<MissingOperationStateMappingDbContext> options)
        : DbContext(options)
    {
        public static MissingOperationStateMappingDbContext Create()
        {
            var options = new DbContextOptionsBuilder<MissingOperationStateMappingDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

            return new MissingOperationStateMappingDbContext(options);
        }
    }
}
