using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Inbox;
using Bondstone.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxQuery_WhenFirstPendingMessageIsLocked_SkipLockedReturnsNextMessage()
    {
        await ResetDatabaseAsync();
        Guid firstMessageId = Guid.Parse("151aa1ba-f54f-49be-88f6-297584db1a4f");
        Guid secondMessageId = Guid.Parse("afec9cab-6a1f-4e30-ab31-4f88f71c94f9");

        await using (PostgreSqlTestDbContext setupContext = CreateContext())
        {
            var firstWriter = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                setupContext,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));
            var secondWriter = new EntityFrameworkCoreDurableOutboxWriter<PostgreSqlTestDbContext>(
                setupContext,
                new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));

            await firstWriter.WriteAsync(CreateEnvelope(firstMessageId));
            await secondWriter.WriteAsync(CreateEnvelope(secondMessageId));
            await setupContext.SaveChangesAsync();
        }

        await using PostgreSqlTestDbContext firstContext = CreateContext();
        await using PostgreSqlTestDbContext secondContext = CreateContext();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction firstTransaction =
            await firstContext.Database.BeginTransactionAsync();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction secondTransaction =
            await secondContext.Database.BeginTransactionAsync();

        Guid lockedMessageId = await SelectNextPendingOutboxMessageForUpdateAsync(firstContext);
        Guid skippedLockedMessageId = await SelectNextPendingOutboxMessageForUpdateAsync(secondContext);

        await firstTransaction.RollbackAsync();
        await secondTransaction.RollbackAsync();

        Assert.Equal(firstMessageId, lockedMessageId);
        Assert.Equal(secondMessageId, skippedLockedMessageId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenPendingMessagesExist_ClaimsDueMessagesAndWritesLeaseState()
    {
        await ResetDatabaseAsync();
        Guid firstMessageId = Guid.Parse("3c1c12e5-7d86-4e28-964d-17b3d0245f9a");
        Guid secondMessageId = Guid.Parse("4d48c716-5574-4f55-ae18-c3a22cc577ef");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-04T00:01:00+00:00");

        await WriteOutboxMessagesAsync(
            (firstMessageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")),
            (secondMessageId, DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));

        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(claimTimeUtc));

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            " dispatcher-1 ",
            TimeSpan.FromMinutes(5),
            maxCount: 1);

        Assert.Single(records);
        DurableOutboxRecord record = records[0];
        Assert.Equal(firstMessageId, record.Envelope.MessageId);
        Assert.Equal(DurableOutboxStatus.Processing, record.DispatchState.Status);
        Assert.Equal(1, record.DispatchState.AttemptCount);
        Assert.Equal("dispatcher-1", record.DispatchState.ClaimedBy);
        Assert.Equal(claimTimeUtc.AddMinutes(5), record.DispatchState.ClaimedUntilUtc);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity firstEntity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == firstMessageId);
        OutboxMessageEntity secondEntity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == secondMessageId);

        Assert.Equal(DurableOutboxStatus.Processing, firstEntity.Status);
        Assert.Equal(1, firstEntity.AttemptCount);
        Assert.Equal("dispatcher-1", firstEntity.ClaimedBy);
        Assert.Equal(claimTimeUtc.AddMinutes(5), firstEntity.ClaimedUntilUtc);
        Assert.Equal(DurableOutboxStatus.Pending, secondEntity.Status);
        Assert.Null(secondEntity.ClaimedBy);
        Assert.Null(secondEntity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenPendingMessagesAreScheduled_ClaimsOnlyDueMessages()
    {
        await ResetDatabaseAsync();
        Guid dueMessageId = Guid.Parse("c5641848-40d1-4061-9084-60c62a270d92");
        Guid futureMessageId = Guid.Parse("d760376e-f1c1-4be0-8bc4-5e5b7f5cd1f8");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-04T00:05:00+00:00");

        await WriteOutboxMessagesAsync(
            (dueMessageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")),
            (futureMessageId, DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));
        await MarkOutboxMessageNextAttemptAsync(
            dueMessageId,
            DateTimeOffset.Parse("2026-06-04T00:04:59+00:00"));
        await MarkOutboxMessageNextAttemptAsync(
            futureMessageId,
            DateTimeOffset.Parse("2026-06-04T00:05:01+00:00"));

        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(claimTimeUtc));

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5),
            maxCount: 10);

        DurableOutboxRecord record = Assert.Single(records);
        Assert.Equal(dueMessageId, record.Envelope.MessageId);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity futureEntity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == futureMessageId);

        Assert.Equal(DurableOutboxStatus.Pending, futureEntity.Status);
        Assert.Null(futureEntity.ClaimedBy);
        Assert.Null(futureEntity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenFirstClaimTransactionIsOpen_SecondClaimerSkipsLockedMessage()
    {
        await ResetDatabaseAsync();
        Guid firstMessageId = Guid.Parse("ad1779e2-70dd-4441-8f9a-25d8a5208f97");
        Guid secondMessageId = Guid.Parse("d8cb565b-b68e-40e5-ac22-4fb2c4da2d5f");

        await WriteOutboxMessagesAsync(
            (firstMessageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")),
            (secondMessageId, DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));

        await using PostgreSqlTestDbContext firstContext = CreateContext();
        await using PostgreSqlTestDbContext secondContext = CreateContext();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction firstTransaction =
            await firstContext.Database.BeginTransactionAsync();
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction secondTransaction =
            await secondContext.Database.BeginTransactionAsync();

        var firstClaimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            firstContext,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:01:00+00:00")));
        var secondClaimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            secondContext,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:01:01+00:00")));

        IReadOnlyList<DurableOutboxRecord> firstClaim = await firstClaimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5),
            maxCount: 1);
        IReadOnlyList<DurableOutboxRecord> secondClaim = await secondClaimer.ClaimAsync(
            "dispatcher-2",
            TimeSpan.FromMinutes(5),
            maxCount: 1);

        await firstTransaction.RollbackAsync();
        await secondTransaction.RollbackAsync();

        DurableOutboxRecord firstRecord = Assert.Single(firstClaim);
        DurableOutboxRecord secondRecord = Assert.Single(secondClaim);
        Assert.Equal(firstMessageId, firstRecord.Envelope.MessageId);
        Assert.Equal(secondMessageId, secondRecord.Envelope.MessageId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenProcessingLeaseExpired_ReclaimsMessage()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("17c57a8e-af36-47e1-9cfd-dbcc0b1d3998");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-04T00:05:00+00:00");

        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));
        await MarkOutboxMessageProcessingAsync(
            messageId,
            "expired-dispatcher",
            DateTimeOffset.Parse("2026-06-04T00:04:59+00:00"));

        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(claimTimeUtc));

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        DurableOutboxRecord record = Assert.Single(records);
        Assert.Equal(messageId, record.Envelope.MessageId);
        Assert.Equal(DurableOutboxStatus.Processing, record.DispatchState.Status);
        Assert.Equal(2, record.DispatchState.AttemptCount);
        Assert.Equal("dispatcher-1", record.DispatchState.ClaimedBy);
        Assert.Equal(claimTimeUtc.AddMinutes(5), record.DispatchState.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxClaimer_WhenProcessingLeaseIsActive_DoesNotClaimMessage()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("397999d9-7e4d-4979-90f1-9d00f03e23a0");

        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));
        await MarkOutboxMessageProcessingAsync(
            messageId,
            "active-dispatcher",
            DateTimeOffset.Parse("2026-06-04T00:05:01+00:00"));

        await using PostgreSqlTestDbContext context = CreateContext();
        var claimer = new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-04T00:05:00+00:00")));

        IReadOnlyList<DurableOutboxRecord> records = await claimer.ClaimAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Empty(records);
    }
}
