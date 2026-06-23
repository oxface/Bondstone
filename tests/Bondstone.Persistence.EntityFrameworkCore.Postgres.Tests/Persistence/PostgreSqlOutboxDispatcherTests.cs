using Bondstone.Configuration;
using Bondstone.Modules;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatcher_WhenTransportSucceeds_DispatchesClaimedMessages()
    {
        await ResetDatabaseAsync();
        Guid firstMessageId = Guid.Parse("5da74cce-19e0-4892-81a3-df0a94b0f91c");
        Guid secondMessageId = Guid.Parse("7bc36a97-2b9f-422b-a363-aaf7e16ca480");
        DateTimeOffset dispatcherTimeUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");

        await WriteOutboxMessagesAsync(
            (firstMessageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")),
            (secondMessageId, DateTimeOffset.Parse("2026-06-04T00:00:02+00:00")));

        RecordingEnvelopeDispatcher transport = new();
        await using PostgreSqlTestDbContext context = CreateContext();
        DurableOutboxDispatcher dispatcher = CreateOutboxDispatcher(
            context,
            transport,
            new FixedTimeProvider(dispatcherTimeUtc));

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            " dispatcher-1 ",
            TimeSpan.FromMinutes(5),
            maxCount: 10);

        Assert.Equal(2, result.ClaimedCount);
        Assert.Equal(2, result.DispatchedCount);
        Assert.Equal(0, result.RetryScheduledCount);
        Assert.Equal(0, result.TerminalFailedCount);
        Assert.Equal(0, result.StaleCount);
        Assert.Equal(2, result.CompletedCount);
        Assert.Equal([firstMessageId, secondMessageId], transport.MessageIds);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        List<OutboxMessageEntity> entities = await verificationContext
            .Set<OutboxMessageEntity>()
            .OrderBy(entity => entity.StoredAtUtc)
            .ToListAsync();

        Assert.Collection(
            entities,
            entity => AssertDispatched(entity, firstMessageId, dispatcherTimeUtc),
            entity => AssertDispatched(entity, secondMessageId, dispatcherTimeUtc));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ModuleOutboxDispatcher_WhenModulesShareTable_ClaimsOnlyItsSourceModuleRows()
    {
        await ResetDatabaseAsync();
        Guid fulfillmentMessageId = Guid.Parse("0345d63b-c115-4fe5-8caf-b2a4a7a48603");
        Guid billingMessageId = Guid.Parse("0b4b5a93-4366-4b40-8d3d-c8a31aef0f54");
        DateTimeOffset dispatcherTimeUtc = DateTimeOffset.Parse("2026-06-18T13:00:00+00:00");

        await WriteOutboxMessageAsync(
            CreateEnvelope(
                fulfillmentMessageId,
                sourceModule: "fulfillment",
                targetModule: "shipping"),
            DateTimeOffset.Parse("2026-06-18T12:00:01+00:00"));
        await WriteOutboxMessageAsync(
            CreateEnvelope(
                billingMessageId,
                sourceModule: "billing",
                targetModule: "shipping"),
            DateTimeOffset.Parse("2026-06-18T12:00:02+00:00"));

        RecordingEnvelopeDispatcher transport = new();
        await using ServiceProvider provider = CreateSharedOutboxModuleProvider(
            transport,
            new FixedTimeProvider(dispatcherTimeUtc));

        DurableOutboxDispatchResult result = await DispatchModuleOutboxAsync(
            provider,
            "fulfillment",
            claimedBy: "fulfillment-dispatcher");

        Assert.Equal(1, result.ClaimedCount);
        Assert.Equal(1, result.DispatchedCount);
        Assert.Equal([fulfillmentMessageId], transport.MessageIds);
        Assert.Equal(["fulfillment"], transport.SourceModules);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        List<OutboxMessageEntity> entities = await verificationContext
            .Set<OutboxMessageEntity>()
            .OrderBy(entity => entity.StoredAtUtc)
            .ToListAsync();

        Assert.Collection(
            entities,
            entity => AssertDispatched(entity, fulfillmentMessageId, dispatcherTimeUtc),
            entity =>
            {
                Assert.Equal(billingMessageId, entity.MessageId);
                Assert.Equal("billing", entity.SourceModule);
                Assert.Equal(DurableOutboxStatus.Pending, entity.Status);
                Assert.Equal(0, entity.AttemptCount);
                Assert.Null(entity.ClaimedBy);
                Assert.Null(entity.ClaimedUntilUtc);
                Assert.Null(entity.DispatchedAtUtc);
            });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatcher_WhenTransportFailsBeforeMaxAttempts_SchedulesRetry()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("e2bb2a7d-870b-4828-b234-059df4756e4d");
        DateTimeOffset dispatcherTimeUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");

        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

        await using PostgreSqlTestDbContext context = CreateContext();
        DurableOutboxDispatcher dispatcher = CreateOutboxDispatcher(
            context,
            new ThrowingEnvelopeDispatcher(new InvalidOperationException("transport unavailable")),
            new FixedTimeProvider(dispatcherTimeUtc),
            new DurableOutboxFailurePolicy(
                maxAttempts: 3,
                retryDelays: [TimeSpan.FromMinutes(2)]));

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(1, result.ClaimedCount);
        Assert.Equal(0, result.DispatchedCount);
        Assert.Equal(1, result.RetryScheduledCount);
        Assert.Equal(0, result.TerminalFailedCount);
        Assert.Equal(0, result.StaleCount);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.Pending, entity.Status);
        Assert.Equal(1, entity.AttemptCount);
        Assert.Equal(dispatcherTimeUtc, entity.FailedAtUtc);
        Assert.Equal(dispatcherTimeUtc.AddMinutes(2), entity.NextAttemptAtUtc);
        Assert.Contains("transport unavailable", entity.FailureReason);
        Assert.Null(entity.DispatchedAtUtc);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatcher_WhenTransportFailsAtMaxAttempts_TerminalFailsMessage()
    {
        await ResetDatabaseAsync();
        Guid messageId = Guid.Parse("adf6d869-11a7-4c9d-a9e4-515ce4a76743");
        DateTimeOffset dispatcherTimeUtc = DateTimeOffset.Parse("2026-06-04T00:02:00+00:00");

        await WriteOutboxMessagesAsync(
            (messageId, DateTimeOffset.Parse("2026-06-04T00:00:01+00:00")));

        await using PostgreSqlTestDbContext context = CreateContext();
        DurableOutboxDispatcher dispatcher = CreateOutboxDispatcher(
            context,
            new ThrowingEnvelopeDispatcher(new InvalidOperationException("poison message")),
            new FixedTimeProvider(dispatcherTimeUtc),
            new DurableOutboxFailurePolicy(maxAttempts: 1));

        DurableOutboxDispatchResult result = await dispatcher.DispatchAsync(
            "dispatcher-1",
            TimeSpan.FromMinutes(5));

        Assert.Equal(1, result.ClaimedCount);
        Assert.Equal(0, result.DispatchedCount);
        Assert.Equal(0, result.RetryScheduledCount);
        Assert.Equal(1, result.TerminalFailedCount);
        Assert.Equal(0, result.StaleCount);

        await using PostgreSqlTestDbContext verificationContext = CreateContext();
        OutboxMessageEntity entity = await verificationContext
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity => entity.MessageId == messageId);

        Assert.Equal(DurableOutboxStatus.TerminalFailed, entity.Status);
        Assert.Equal(1, entity.AttemptCount);
        Assert.Equal(dispatcherTimeUtc, entity.FailedAtUtc);
        Assert.Contains("poison message", entity.FailureReason);
        Assert.Null(entity.NextAttemptAtUtc);
        Assert.Null(entity.DispatchedAtUtc);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    private static DurableOutboxDispatcher CreateOutboxDispatcher(
        PostgreSqlTestDbContext context,
        IDurableEnvelopeDispatcher dispatcher,
        TimeProvider timeProvider,
        IDurableOutboxFailurePolicy? failurePolicy = null)
    {
        return new DurableOutboxDispatcher(
            new PostgreSqlDurableOutboxClaimer<PostgreSqlTestDbContext>(context, timeProvider),
            new PostgreSqlDurableOutboxLeaseRenewer<PostgreSqlTestDbContext>(context, timeProvider),
            dispatcher,
            failurePolicy ?? new DurableOutboxFailurePolicy(),
            new PostgreSqlDurableOutboxDispatchRecorder<PostgreSqlTestDbContext>(context),
            timeProvider);
    }

    private ServiceProvider CreateSharedOutboxModuleProvider(
        IDurableEnvelopeDispatcher envelopeDispatcher,
        TimeProvider timeProvider)
    {
        var services = new ServiceCollection();
        services.AddSingleton(timeProvider);
        services.AddSingleton(envelopeDispatcher);
        services.AddSingleton<IDurableOutboxFailurePolicy>(
            new DurableOutboxFailurePolicy());

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<PostgreSqlTestDbContext>(
                    $"{_fixture.ConnectionString};Pooling=false");
            });

            bondstone.Module("billing", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<PostgreSqlTestDbContext>(
                    $"{_fixture.ConnectionString};Pooling=false");
            });
        });

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    private static async Task<DurableOutboxDispatchResult> DispatchModuleOutboxAsync(
        IServiceProvider provider,
        string moduleName,
        string claimedBy)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DurableModulePersistenceRegistrationRegistry registry = scope.ServiceProvider
            .GetRequiredService<DurableModulePersistenceRegistrationRegistry>();
        DurableModuleOutboxDispatcherRegistration registration = Assert.Single(
            registry.OutboxDispatcherRegistrations,
            candidate => candidate.ModuleName == moduleName);
        IDurableOutboxDispatcher dispatcher = registration.CreateDispatcher(scope.ServiceProvider);

        return await dispatcher.DispatchAsync(
            claimedBy,
            TimeSpan.FromMinutes(5),
            maxCount: 10);
    }

    private static void AssertDispatched(
        OutboxMessageEntity entity,
        Guid messageId,
        DateTimeOffset dispatchedAtUtc)
    {
        Assert.Equal(messageId, entity.MessageId);
        Assert.Equal(DurableOutboxStatus.Dispatched, entity.Status);
        Assert.Equal(1, entity.AttemptCount);
        Assert.Equal(dispatchedAtUtc, entity.DispatchedAtUtc);
        Assert.Null(entity.NextAttemptAtUtc);
        Assert.Null(entity.FailedAtUtc);
        Assert.Null(entity.FailureReason);
        Assert.Null(entity.ClaimedBy);
        Assert.Null(entity.ClaimedUntilUtc);
    }

    private sealed class RecordingEnvelopeDispatcher : IDurableEnvelopeDispatcher
    {
        private readonly List<Guid> _messageIds = [];
        private readonly List<string> _sourceModules = [];

        public IReadOnlyList<Guid> MessageIds => _messageIds;

        public IReadOnlyList<string> SourceModules => _sourceModules;

        public ValueTask DispatchAsync(
            DurableOutboxRecord record,
            CancellationToken ct = default)
        {
            _messageIds.Add(record.Envelope.MessageId);
            _sourceModules.Add(record.Envelope.SourceModule);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingEnvelopeDispatcher(Exception exception) : IDurableEnvelopeDispatcher
    {
        public ValueTask DispatchAsync(
            DurableOutboxRecord record,
            CancellationToken ct = default)
        {
            throw exception;
        }
    }
}
