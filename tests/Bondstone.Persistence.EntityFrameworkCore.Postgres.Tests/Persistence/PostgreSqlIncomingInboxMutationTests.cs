using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.IncomingInbox;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxClaimer_WhenPendingRowsExist_ClaimsInDeterministicOrder()
    {
        await ResetIncomingInboxDatabaseAsync();
        Guid firstMessageId = Guid.Parse("9f4a3052-6071-4c28-a4b4-66cd41147818");
        Guid secondMessageId = Guid.Parse("8ebc86d4-7bdc-4bf0-a1cb-71f9a6e52db0");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-17T00:05:00+00:00");

        await WriteIncomingInboxRecordsAsync(
            CreateIncomingInboxRecord(
                secondMessageId,
                DurableIncomingInboxStatus.Pending,
                DateTimeOffset.Parse("2026-06-17T00:00:02+00:00")),
            CreateIncomingInboxRecord(
                firstMessageId,
                DurableIncomingInboxStatus.Pending,
                DateTimeOffset.Parse("2026-06-17T00:00:01+00:00")));

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var claimer = new PostgreSqlDurableIncomingInboxClaimer<PostgreSqlIncomingInboxTestDbContext>(
            context,
            new FixedTimeProvider(claimTimeUtc));

        IReadOnlyList<DurableIncomingInboxRecord> records = await claimer.ClaimAsync(
            " worker-1 ",
            TimeSpan.FromMinutes(5),
            maxCount: 10);

        Assert.Collection(
            records,
            first => Assert.Equal(firstMessageId, first.Key.MessageId),
            second => Assert.Equal(secondMessageId, second.Key.MessageId));
        Assert.All(
            records,
            record =>
            {
                Assert.Equal(DurableIncomingInboxStatus.Processing, record.State.Status);
                Assert.Equal(1, record.State.AttemptCount);
                Assert.Equal("worker-1", record.State.ClaimedBy);
                Assert.Equal(claimTimeUtc.AddMinutes(5), record.State.ClaimedUntilUtc);
                Assert.Null(record.State.NextAttemptAtUtc);
                Assert.Null(record.State.FailedAtUtc);
                Assert.Null(record.State.FailureReason);
            });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxClaimer_WhenRetryRowsAreDue_ClaimsAndClearsFailureOutcome()
    {
        await ResetIncomingInboxDatabaseAsync();
        Guid messageId = Guid.Parse("37c43cd7-13ac-4d8c-8982-a97c91ae7a0c");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-17T00:05:00+00:00");

        await WriteIncomingInboxRecordsAsync(CreateIncomingInboxRecord(
            messageId,
            DurableIncomingInboxStatus.RetryScheduled,
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
            attemptCount: 1,
            failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
            nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-17T00:04:59+00:00")));

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var claimer = new PostgreSqlDurableIncomingInboxClaimer<PostgreSqlIncomingInboxTestDbContext>(
            context,
            new FixedTimeProvider(claimTimeUtc));

        DurableIncomingInboxRecord record = Assert.Single(await claimer.ClaimAsync(
            "worker-1",
            TimeSpan.FromMinutes(5)));

        Assert.Equal(messageId, record.Key.MessageId);
        Assert.Equal(DurableIncomingInboxStatus.Processing, record.State.Status);
        Assert.Equal(2, record.State.AttemptCount);
        Assert.Null(record.State.NextAttemptAtUtc);
        Assert.Null(record.State.FailedAtUtc);
        Assert.Null(record.State.FailureReason);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxClaimer_WhenRetryRowsAreNotDue_DoesNotClaim()
    {
        await ResetIncomingInboxDatabaseAsync();
        Guid messageId = Guid.Parse("ddfd4dc6-2845-4f95-a1d0-09d8dd9fefbd");

        await WriteIncomingInboxRecordsAsync(CreateIncomingInboxRecord(
            messageId,
            DurableIncomingInboxStatus.RetryScheduled,
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
            attemptCount: 1,
            failedAtUtc: DateTimeOffset.Parse("2026-06-17T00:01:00+00:00"),
            nextAttemptAtUtc: DateTimeOffset.Parse("2026-06-17T00:05:01+00:00")));

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var claimer = new PostgreSqlDurableIncomingInboxClaimer<PostgreSqlIncomingInboxTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-17T00:05:00+00:00")));

        IReadOnlyList<DurableIncomingInboxRecord> records = await claimer.ClaimAsync(
            "worker-1",
            TimeSpan.FromMinutes(5));

        Assert.Empty(records);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxClaimer_WhenProcessingLeaseIsStale_ReclaimsRow()
    {
        await ResetIncomingInboxDatabaseAsync();
        Guid messageId = Guid.Parse("65026811-2de8-4e9f-8ca2-99e391b2dc3f");
        DateTimeOffset claimTimeUtc = DateTimeOffset.Parse("2026-06-17T00:05:00+00:00");

        await WriteIncomingInboxRecordsAsync(CreateIncomingInboxRecord(
            messageId,
            DurableIncomingInboxStatus.Processing,
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
            attemptCount: 1,
            claimedBy: "stale-worker",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00")));

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var claimer = new PostgreSqlDurableIncomingInboxClaimer<PostgreSqlIncomingInboxTestDbContext>(
            context,
            new FixedTimeProvider(claimTimeUtc));

        DurableIncomingInboxRecord record = Assert.Single(await claimer.ClaimAsync(
            "worker-1",
            TimeSpan.FromMinutes(5)));

        Assert.Equal(messageId, record.Key.MessageId);
        Assert.Equal(DurableIncomingInboxStatus.Processing, record.State.Status);
        Assert.Equal(2, record.State.AttemptCount);
        Assert.Equal("worker-1", record.State.ClaimedBy);
        Assert.Equal(claimTimeUtc.AddMinutes(5), record.State.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxClaimer_WhenProcessingLeaseIsActive_DoesNotClaim()
    {
        await ResetIncomingInboxDatabaseAsync();

        await WriteIncomingInboxRecordsAsync(CreateIncomingInboxRecord(
            Guid.Parse("dbb5d50e-70f0-41f8-8e32-a1ee146ec573"),
            DurableIncomingInboxStatus.Processing,
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
            attemptCount: 1,
            claimedBy: "active-worker",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:01+00:00")));

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var claimer = new PostgreSqlDurableIncomingInboxClaimer<PostgreSqlIncomingInboxTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-17T00:05:00+00:00")));

        IReadOnlyList<DurableIncomingInboxRecord> records = await claimer.ClaimAsync(
            "worker-1",
            TimeSpan.FromMinutes(5));

        Assert.Empty(records);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxLeaseRenewer_WhenClaimIsActive_ExtendsLease()
    {
        await ResetIncomingInboxDatabaseAsync();
        Guid messageId = Guid.Parse("9480bc40-7dec-4bf4-8a6e-a5d49c19a350");
        DateTimeOffset renewalTimeUtc = DateTimeOffset.Parse("2026-06-17T00:02:00+00:00");

        DurableIncomingInboxRecord seeded = CreateIncomingInboxRecord(
            messageId,
            DurableIncomingInboxStatus.Processing,
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
            attemptCount: 1,
            claimedBy: "worker-1",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:05:00+00:00"));
        await WriteIncomingInboxRecordsAsync(seeded);

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var renewer = new PostgreSqlDurableIncomingInboxLeaseRenewer<PostgreSqlIncomingInboxTestDbContext>(
            context,
            new FixedTimeProvider(renewalTimeUtc));

        bool renewed = await renewer.RenewAsync(
            seeded.Key,
            " worker-1 ",
            TimeSpan.FromMinutes(10));

        Assert.True(renewed);

        IncomingInboxMessageEntity entity = await ReadIncomingInboxEntityAsync(seeded.Key);
        Assert.Equal(DurableIncomingInboxStatus.Processing, entity.Status);
        Assert.Equal("worker-1", entity.ClaimedBy);
        Assert.Equal(renewalTimeUtc.AddMinutes(10), entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxLeaseRenewer_WhenClaimIsStale_ReturnsFalse()
    {
        await ResetIncomingInboxDatabaseAsync();
        DurableIncomingInboxRecord seeded = CreateIncomingInboxRecord(
            Guid.Parse("742c4352-e3c6-427b-a758-7f161502f7e4"),
            DurableIncomingInboxStatus.Processing,
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
            attemptCount: 1,
            claimedBy: "worker-1",
            claimedUntilUtc: DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"));
        await WriteIncomingInboxRecordsAsync(seeded);

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var renewer = new PostgreSqlDurableIncomingInboxLeaseRenewer<PostgreSqlIncomingInboxTestDbContext>(
            context,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-17T00:02:00+00:00")));

        bool renewed = await renewer.RenewAsync(
            seeded.Key,
            "worker-1",
            TimeSpan.FromMinutes(10));

        Assert.False(renewed);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxOutcomeRecorder_WhenClaimedRowIsProcessed_MarksProcessedAndClearsClaim()
    {
        await ResetIncomingInboxDatabaseAsync();
        DateTimeOffset processedAtUtc = DateTimeOffset.Parse("2026-06-17T00:02:00+00:00");
        DurableIncomingInboxRecord seeded = await WriteClaimedIncomingInboxRecordAsync(
            Guid.Parse("07c6fc18-d89c-4264-94c7-433de96c2bb0"),
            "worker-1");

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var recorder = new PostgreSqlDurableIncomingInboxOutcomeRecorder<PostgreSqlIncomingInboxTestDbContext>(context);

        bool updated = await recorder.MarkProcessedAsync(
            seeded.Key,
            "worker-1",
            processedAtUtc);

        Assert.True(updated);

        IncomingInboxMessageEntity entity = await ReadIncomingInboxEntityAsync(seeded.Key);
        Assert.Equal(DurableIncomingInboxStatus.Processed, entity.Status);
        Assert.Equal(processedAtUtc, entity.ProcessedAtUtc);
        Assert.Null(entity.NextAttemptAtUtc);
        Assert.Null(entity.FailedAtUtc);
        Assert.Null(entity.FailureReason);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxOutcomeRecorder_WhenClaimIsStale_DoesNotMarkProcessed()
    {
        await ResetIncomingInboxDatabaseAsync();
        DurableIncomingInboxRecord seeded = await WriteClaimedIncomingInboxRecordAsync(
            Guid.Parse("f7e7fcf9-a11c-4a14-be02-7b23cc006825"),
            "worker-1",
            DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"));

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var recorder = new PostgreSqlDurableIncomingInboxOutcomeRecorder<PostgreSqlIncomingInboxTestDbContext>(context);

        bool updated = await recorder.MarkProcessedAsync(
            seeded.Key,
            "worker-1",
            DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"));

        Assert.False(updated);

        IncomingInboxMessageEntity entity = await ReadIncomingInboxEntityAsync(seeded.Key);
        Assert.Equal(DurableIncomingInboxStatus.Processing, entity.Status);
        Assert.Equal("worker-1", entity.ClaimedBy);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxOutcomeRecorder_WhenClaimedRowFails_SchedulesRetryAndClearsClaim()
    {
        await ResetIncomingInboxDatabaseAsync();
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-17T00:02:00+00:00");
        DateTimeOffset nextAttemptAtUtc = DateTimeOffset.Parse("2026-06-17T00:07:00+00:00");
        DurableIncomingInboxRecord seeded = await WriteClaimedIncomingInboxRecordAsync(
            Guid.Parse("f7f6fc1f-7948-4a5d-ac64-4825f8386502"),
            "worker-1");

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var recorder = new PostgreSqlDurableIncomingInboxOutcomeRecorder<PostgreSqlIncomingInboxTestDbContext>(context);

        bool updated = await recorder.ScheduleRetryAsync(
            seeded.Key,
            "worker-1",
            " handler unavailable ",
            failedAtUtc,
            nextAttemptAtUtc);

        Assert.True(updated);

        IncomingInboxMessageEntity entity = await ReadIncomingInboxEntityAsync(seeded.Key);
        Assert.Equal(DurableIncomingInboxStatus.RetryScheduled, entity.Status);
        Assert.Equal(failedAtUtc, entity.FailedAtUtc);
        Assert.Equal(nextAttemptAtUtc, entity.NextAttemptAtUtc);
        Assert.Equal("handler unavailable", entity.FailureReason);
        Assert.Null(entity.ProcessedAtUtc);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxOutcomeRecorder_WhenClaimIsStale_DoesNotScheduleRetry()
    {
        await ResetIncomingInboxDatabaseAsync();
        DurableIncomingInboxRecord seeded = await WriteClaimedIncomingInboxRecordAsync(
            Guid.Parse("029cf301-bcca-4787-9fd9-7e5857f3594d"),
            "worker-1",
            DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"));

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var recorder = new PostgreSqlDurableIncomingInboxOutcomeRecorder<PostgreSqlIncomingInboxTestDbContext>(context);

        bool updated = await recorder.ScheduleRetryAsync(
            seeded.Key,
            "worker-1",
            "handler unavailable",
            DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"),
            DateTimeOffset.Parse("2026-06-17T00:07:00+00:00"));

        Assert.False(updated);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxOutcomeRecorder_WhenClaimedRowTerminallyFails_MarksTerminalFailedAndClearsClaim()
    {
        await ResetIncomingInboxDatabaseAsync();
        DateTimeOffset failedAtUtc = DateTimeOffset.Parse("2026-06-17T00:02:00+00:00");
        DurableIncomingInboxRecord seeded = await WriteClaimedIncomingInboxRecordAsync(
            Guid.Parse("e5b8bf66-d182-41f1-9170-99213f822145"),
            "worker-1");

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var recorder = new PostgreSqlDurableIncomingInboxOutcomeRecorder<PostgreSqlIncomingInboxTestDbContext>(context);

        bool updated = await recorder.MarkTerminalFailedAsync(
            seeded.Key,
            "worker-1",
            "poison message",
            failedAtUtc);

        Assert.True(updated);

        IncomingInboxMessageEntity entity = await ReadIncomingInboxEntityAsync(seeded.Key);
        Assert.Equal(DurableIncomingInboxStatus.TerminalFailed, entity.Status);
        Assert.Equal(failedAtUtc, entity.FailedAtUtc);
        Assert.Equal("poison message", entity.FailureReason);
        Assert.Null(entity.NextAttemptAtUtc);
        Assert.Null(entity.ProcessedAtUtc);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxOutcomeRecorder_WhenClaimIsStale_DoesNotMarkTerminalFailed()
    {
        await ResetIncomingInboxDatabaseAsync();
        DurableIncomingInboxRecord seeded = await WriteClaimedIncomingInboxRecordAsync(
            Guid.Parse("fcf061a0-3f0a-44c4-adf2-2331a428d231"),
            "worker-1",
            DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"));

        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        var recorder = new PostgreSqlDurableIncomingInboxOutcomeRecorder<PostgreSqlIncomingInboxTestDbContext>(context);

        bool updated = await recorder.MarkTerminalFailedAsync(
            seeded.Key,
            "worker-1",
            "poison message",
            DateTimeOffset.Parse("2026-06-17T00:02:00+00:00"));

        Assert.False(updated);
    }

    private async Task ResetIncomingInboxDatabaseAsync()
    {
        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private PostgreSqlIncomingInboxTestDbContext CreateIncomingInboxContext()
    {
        var options = new DbContextOptionsBuilder<PostgreSqlIncomingInboxTestDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new PostgreSqlIncomingInboxTestDbContext(options);
    }

    private async Task WriteIncomingInboxRecordsAsync(
        params DurableIncomingInboxRecord[] records)
    {
        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();

        context.Set<IncomingInboxMessageEntity>().AddRange(
            records.Select(IncomingInboxMessageEntity.FromRecord));
        await context.SaveChangesAsync();
    }

    private async Task<DurableIncomingInboxRecord> WriteClaimedIncomingInboxRecordAsync(
        Guid messageId,
        string claimedBy,
        DateTimeOffset? claimedUntilUtc = null)
    {
        DurableIncomingInboxRecord record = CreateIncomingInboxRecord(
            messageId,
            DurableIncomingInboxStatus.Processing,
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"),
            attemptCount: 1,
            claimedBy: claimedBy,
            claimedUntilUtc: claimedUntilUtc ?? DateTimeOffset.Parse("2026-06-17T00:05:00+00:00"));

        await WriteIncomingInboxRecordsAsync(record);

        return record;
    }

    private async Task<IncomingInboxMessageEntity> ReadIncomingInboxEntityAsync(
        DurableIncomingInboxKey key)
    {
        await using PostgreSqlIncomingInboxTestDbContext context = CreateIncomingInboxContext();

        return await context
            .Set<IncomingInboxMessageEntity>()
            .SingleAsync(entity =>
                entity.ReceiverModule == key.ReceiverModule
                && entity.MessageId == key.MessageId
                && entity.HandlerIdentity == key.HandlerIdentity);
    }

    private static DurableIncomingInboxRecord CreateIncomingInboxRecord(
        Guid messageId,
        DurableIncomingInboxStatus status,
        DateTimeOffset ingestedAtUtc,
        int attemptCount = 0,
        DateTimeOffset? failedAtUtc = null,
        DateTimeOffset? nextAttemptAtUtc = null,
        string? claimedBy = null,
        DateTimeOffset? claimedUntilUtc = null)
    {
        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Command,
            "fulfillment.receive.v1",
            "ordering",
            "fulfillment",
            "{}",
            DateTimeOffset.Parse("2026-06-17T00:00:00+00:00"));
        var state = new DurableIncomingInboxState(
            status,
            attemptCount,
            nextAttemptAtUtc,
            processedAtUtc: null,
            failedAtUtc,
            status is DurableIncomingInboxStatus.RetryScheduled or DurableIncomingInboxStatus.TerminalFailed
                ? "receive failed"
                : null,
            claimedBy,
            claimedUntilUtc);

        return new DurableIncomingInboxRecord(
            DurableIncomingInboxKey.ForCommandHandler(
                envelope.MessageId,
                "fulfillment",
                "fulfillment.receive.v1"),
            envelope,
            ingestedAtUtc,
            state,
            "rabbitmq:orders");
    }
}
