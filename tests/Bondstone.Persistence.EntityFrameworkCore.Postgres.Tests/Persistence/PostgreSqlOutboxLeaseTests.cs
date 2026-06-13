using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxLeaseRenewer_WhenClaimIsActive_ExtendsLease()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("5addcc0d-d161-4e78-9d90-82a7cbd98df1");
        DateTimeOffset renewalTimeUtc = DateTimeOffset.Parse("2026-06-04T00:03:00+00:00");

        await WriteClaimedOutboxMessageAsync(
            messageId,
            "dispatcher-1",
            DateTimeOffset.Parse("2026-06-04T00:05:00+00:00"));

        await using PostgreSqlTestDbContext context = CreateContext();
        var renewer = new PostgreSqlDurableOutboxLeaseRenewer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(renewalTimeUtc));

        bool renewed = await renewer.RenewAsync(
            messageId,
            " dispatcher-1 ",
            TimeSpan.FromMinutes(10));

        Assert.True(renewed);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.Processing, entity.Status);
        Assert.Equal("dispatcher-1", entity.ClaimedBy);
        Assert.Equal(renewalTimeUtc.AddMinutes(10), entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxLeaseRenewer_WhenClaimIsOwnedByAnotherWorker_ReturnsFalse()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("9ded6d5a-9a1c-4ea0-86f5-f38816a5ea5f");
        DateTimeOffset originalClaimedUntilUtc = DateTimeOffset.Parse("2026-06-04T00:05:00+00:00");

        await WriteClaimedOutboxMessageAsync(messageId, "dispatcher-1", originalClaimedUntilUtc);

        await using PostgreSqlTestDbContext context = CreateContext();
        var renewer = new PostgreSqlDurableOutboxLeaseRenewer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:03:00+00:00")));

        bool renewed = await renewer.RenewAsync(
            messageId,
            "dispatcher-2",
            TimeSpan.FromMinutes(10));

        Assert.False(renewed);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal("dispatcher-1", entity.ClaimedBy);
        Assert.Equal(originalClaimedUntilUtc, entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxLeaseRenewer_WhenClaimLeaseExpired_ReturnsFalse()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("c6cc5598-3e1f-4a3e-97ed-f76df5a461a2");
        DateTimeOffset originalClaimedUntilUtc = DateTimeOffset.Parse("2026-06-04T00:01:59+00:00");

        await WriteClaimedOutboxMessageAsync(messageId, "dispatcher-1", originalClaimedUntilUtc);

        await using PostgreSqlTestDbContext context = CreateContext();
        var renewer = new PostgreSqlDurableOutboxLeaseRenewer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:02:00+00:00")));

        bool renewed = await renewer.RenewAsync(
            messageId,
            "dispatcher-1",
            TimeSpan.FromMinutes(10));

        Assert.False(renewed);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.Processing, entity.Status);
        Assert.Equal(originalClaimedUntilUtc, entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxLeaseRenewer_WhenMessageIsNotProcessing_ReturnsFalse()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("ca7c6464-5445-47d3-895d-f1da63d7e70c");

        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

        await using PostgreSqlTestDbContext context = CreateContext();
        var renewer = new PostgreSqlDurableOutboxLeaseRenewer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:02:00+00:00")));

        bool renewed = await renewer.RenewAsync(
            messageId,
            "dispatcher-1",
            TimeSpan.FromMinutes(10));

        Assert.False(renewed);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.Pending, entity.Status);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }
}
