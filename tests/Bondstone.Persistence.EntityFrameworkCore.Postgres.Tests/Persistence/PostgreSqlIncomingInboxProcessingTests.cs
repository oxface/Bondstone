using Bondstone.Configuration;
using Bondstone.DomainEvents;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.DomainEvents;
using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.IncomingInbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed class PostgreSqlIncomingInboxProcessingTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>
{
    private static readonly DateTimeOffset UtcNow =
        DateTimeOffset.Parse("2026-06-18T12:00:00+00:00");

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxDispatcher_WhenCommandRowIsPending_InvokesModulePipelineAndRecordsProcessedOutcome()
    {
        await using ServiceProvider provider = CreateProvider();
        await ResetDatabaseAsync(provider);
        Guid messageId = Guid.Parse("2d5215f4-edda-40b9-98fa-292918b2db6b");
        Guid operationId = Guid.Parse("8ea28e52-2131-4f13-98b3-7d83c15faf56");
        DurableIncomingInboxRecord record = CreateCommandRecord(
            messageId,
            new ReserveInventoryCommand("A-100"),
            durableOperationId: operationId);

        DurableIncomingInboxIngestionResult ingestionResult = await IngestAsync(provider, record);
        DurableIncomingInboxProcessingResult processingResult = await ProcessAsync(provider);

        Assert.Equal(DurableIncomingInboxIngestionStatus.Ingested, ingestionResult.Status);
        Assert.Equal(1, processingResult.ClaimedCount);
        Assert.Equal(1, processingResult.ProcessedCount);
        Assert.Equal(0, processingResult.RetryScheduledCount);
        Assert.Equal(0, processingResult.TerminalFailedCount);

        await using AsyncServiceScope verificationScope = provider.CreateAsyncScope();
        IncomingInboxProcessingTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<IncomingInboxProcessingTestDbContext>();

        IncomingInboxMessageEntity incoming = await context.Set<IncomingInboxMessageEntity>().SingleAsync();
        Assert.Equal(DurableIncomingInboxStatus.Processed, incoming.Status);
        Assert.Equal(1, incoming.AttemptCount);
        Assert.NotNull(incoming.ProcessedAtUtc);
        Assert.Null(incoming.ClaimedBy);

        InboxMessageEntity inbox = await context.Set<InboxMessageEntity>().SingleAsync();
        Assert.Equal(messageId, inbox.MessageId);
        Assert.Equal("fulfillment", inbox.ModuleName);
        Assert.Equal("fulfillment.reserve-inventory.v1", inbox.HandlerIdentity);
        Assert.NotNull(inbox.ProcessedAtUtc);

        HandledMessageEntity handled = await context.HandledMessages.SingleAsync();
        Assert.Equal("reserve:A-100", handled.Id);

        OutboxMessageEntity outbox = await context.Set<OutboxMessageEntity>().SingleAsync();
        Assert.Equal(DurableOutboxStatus.Pending, outbox.Status);
        Assert.Equal("fulfillment", outbox.SourceModule);
        Assert.Equal("fulfillment.inventory-reserved.v1", outbox.MessageTypeName);

        OperationStateEntity operation = await context.Set<OperationStateEntity>().SingleAsync();
        Assert.Equal(operationId, operation.DurableOperationId);
        Assert.Equal(DurableOperationStatus.Completed, operation.Status);
        Assert.Contains("A-100", operation.ResultPayload, StringComparison.Ordinal);
        Assert.Equal("fulfillment", operation.ModuleName);
        Assert.Equal("fulfillment.reserve-inventory.v1", operation.HandlerIdentity);

        DomainEventAggregateEntity aggregate = await context.DomainEventAggregates.SingleAsync();
        Assert.Equal("A-100", aggregate.Id);
        DomainEventRecordEntity domainEvent = await context.Set<DomainEventRecordEntity>().SingleAsync();
        Assert.Equal("fulfillment", domainEvent.ModuleName);
        Assert.Equal("fulfillment.inventory-reserved.domain.v1", domainEvent.DomainEventName);
        Assert.Contains("\"inventoryId\":\"A-100\"", domainEvent.Payload, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxDispatcher_WhenDuplicateDeliveryIsAlreadyIngested_DoesNotRerunHandler()
    {
        await using ServiceProvider provider = CreateProvider();
        await ResetDatabaseAsync(provider);
        Guid messageId = Guid.Parse("b371a265-aa3f-42f2-8dd9-05e7b351c3bd");
        DurableIncomingInboxRecord record = CreateCommandRecord(
            messageId,
            new ReserveInventoryCommand("A-200"));

        DurableIncomingInboxIngestionResult firstIngestion = await IngestAsync(provider, record);
        DurableIncomingInboxIngestionResult duplicateIngestion = await IngestAsync(provider, record);
        DurableIncomingInboxProcessingResult firstProcessing = await ProcessAsync(provider);
        DurableIncomingInboxProcessingResult secondProcessing = await ProcessAsync(provider);

        Assert.Equal(DurableIncomingInboxIngestionStatus.Ingested, firstIngestion.Status);
        Assert.Equal(DurableIncomingInboxIngestionStatus.AlreadyIngested, duplicateIngestion.Status);
        Assert.Equal(1, firstProcessing.ProcessedCount);
        Assert.Equal(0, secondProcessing.ClaimedCount);

        await using AsyncServiceScope verificationScope = provider.CreateAsyncScope();
        IncomingInboxProcessingTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<IncomingInboxProcessingTestDbContext>();

        Assert.Equal(1, await context.HandledMessages.CountAsync());
        Assert.Equal(1, await context.Set<IncomingInboxMessageEntity>().CountAsync());
        Assert.Equal(1, await context.Set<InboxMessageEntity>().CountAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ModuleIncomingInboxDispatcher_WhenModulesShareTable_ClaimsOnlyItsReceiverModuleRows()
    {
        await using ServiceProvider provider = CreateSharedModuleProvider();
        await ResetDatabaseAsync(provider);
        DurableIncomingInboxRecord billingRecord = CreateBillingCommandRecord(
            Guid.Parse("c214e076-44e8-49db-becb-409df74fcf3b"),
            new CapturePaymentCommand("A-250"));

        await IngestAsync(provider, billingRecord);
        DurableIncomingInboxProcessingResult fulfillmentResult =
            await ProcessModuleAsync(provider, "fulfillment");
        DurableIncomingInboxProcessingResult billingResult =
            await ProcessModuleAsync(provider, "billing");

        Assert.Equal(0, fulfillmentResult.ClaimedCount);
        Assert.Equal(1, billingResult.ClaimedCount);
        Assert.Equal(1, billingResult.ProcessedCount);

        await using AsyncServiceScope verificationScope = provider.CreateAsyncScope();
        IncomingInboxProcessingTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<IncomingInboxProcessingTestDbContext>();

        IncomingInboxMessageEntity incoming = await context.Set<IncomingInboxMessageEntity>().SingleAsync();
        Assert.Equal("billing", incoming.ReceiverModule);
        Assert.Equal(DurableIncomingInboxStatus.Processed, incoming.Status);
        HandledMessageEntity handled = await context.HandledMessages.SingleAsync();
        Assert.Equal("billing:A-250", handled.Id);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxDispatcher_WhenHandlerFailsBeforeMaxAttempts_RecordsRetryOutcome()
    {
        await using ServiceProvider provider = CreateProvider(
            new DurableIncomingInboxProcessingOptions(
                maxAttempts: 2,
                retryDelays: [TimeSpan.FromMinutes(1)]));
        await ResetDatabaseAsync(provider);
        Guid operationId = Guid.Parse("5ee45ffc-edfc-4627-85ec-612f4a0c739e");
        DurableIncomingInboxRecord record = CreateCommandRecord(
            Guid.Parse("ac995e64-329d-46f4-a3ce-d2e782f531e5"),
            new FailInventoryCommand("A-300"),
            durableOperationId: operationId);

        await IngestAsync(provider, record);
        DurableIncomingInboxProcessingResult processingResult = await ProcessAsync(provider);

        Assert.Equal(1, processingResult.ClaimedCount);
        Assert.Equal(0, processingResult.ProcessedCount);
        Assert.Equal(1, processingResult.RetryScheduledCount);
        Assert.Equal(0, processingResult.TerminalFailedCount);

        await using AsyncServiceScope verificationScope = provider.CreateAsyncScope();
        IncomingInboxProcessingTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<IncomingInboxProcessingTestDbContext>();

        IncomingInboxMessageEntity incoming = await context.Set<IncomingInboxMessageEntity>().SingleAsync();
        Assert.Equal(DurableIncomingInboxStatus.RetryScheduled, incoming.Status);
        Assert.Equal(1, incoming.AttemptCount);
        Assert.Equal(UtcNow.AddMinutes(1), incoming.NextAttemptAtUtc);
        Assert.Equal(UtcNow, incoming.FailedAtUtc);
        Assert.Contains("boom:A-300", incoming.FailureReason, StringComparison.Ordinal);
        Assert.Empty(await context.Set<InboxMessageEntity>().ToListAsync());
        Assert.Empty(await context.HandledMessages.ToListAsync());
        Assert.Empty(await context.Set<OutboxMessageEntity>().ToListAsync());
        Assert.Empty(await context.Set<OperationStateEntity>().ToListAsync());
        Assert.Empty(await context.Set<DomainEventRecordEntity>().ToListAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxDispatcher_WhenHandlerFailsAtMaxAttempts_RecordsTerminalOutcome()
    {
        await using ServiceProvider provider = CreateProvider(
            new DurableIncomingInboxProcessingOptions(maxAttempts: 1));
        await ResetDatabaseAsync(provider);
        DurableIncomingInboxRecord record = CreateCommandRecord(
            Guid.Parse("f1d1226c-a2b5-4803-a2ba-dcc875e4a396"),
            new FailInventoryCommand("A-400"));

        await IngestAsync(provider, record);
        DurableIncomingInboxProcessingResult processingResult = await ProcessAsync(provider);

        Assert.Equal(1, processingResult.ClaimedCount);
        Assert.Equal(0, processingResult.ProcessedCount);
        Assert.Equal(0, processingResult.RetryScheduledCount);
        Assert.Equal(1, processingResult.TerminalFailedCount);

        await using AsyncServiceScope verificationScope = provider.CreateAsyncScope();
        IncomingInboxProcessingTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<IncomingInboxProcessingTestDbContext>();

        IncomingInboxMessageEntity incoming = await context.Set<IncomingInboxMessageEntity>().SingleAsync();
        Assert.Equal(DurableIncomingInboxStatus.TerminalFailed, incoming.Status);
        Assert.Equal(1, incoming.AttemptCount);
        Assert.Null(incoming.NextAttemptAtUtc);
        Assert.Equal(UtcNow, incoming.FailedAtUtc);
        Assert.Contains("boom:A-400", incoming.FailureReason, StringComparison.Ordinal);
        Assert.Empty(await context.Set<OperationStateEntity>().ToListAsync());

        IDurableIncomingInboxInspectionStore inspectionStore =
            CreateIncomingInspectionStore(verificationScope.ServiceProvider);
        DurableIncomingInboxRecord terminal = Assert.Single(
            await inspectionStore.FindTerminalFailedAsync(
                failedAtOrBeforeUtc: UtcNow,
                receiverModule: "fulfillment",
                sourceTransportName: "rabbitmq:fulfillment.commands"));
        Assert.Equal(DurableIncomingInboxStatus.TerminalFailed, terminal.State.Status);
        Assert.Equal("fulfillment", terminal.ReceiverModule);
        Assert.Equal("fulfillment.fail-inventory.v1", terminal.HandlerIdentity);
        Assert.Equal(MessageKind.Command, terminal.Envelope.MessageKind);
        Assert.Equal("fulfillment.fail-inventory.v1", terminal.Envelope.MessageTypeName);
        Assert.Equal("rabbitmq:fulfillment.commands", terminal.SourceTransportName);
        Assert.Contains("boom:A-400", terminal.State.FailureReason, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task IncomingInboxInspectionStore_WhenProcessingClaimIsStale_ReturnsStaleEvidence()
    {
        await using ServiceProvider provider = CreateProvider();
        await ResetDatabaseAsync(provider);
        DurableIncomingInboxRecord staleRecord = CreateCommandRecord(
            Guid.Parse("51b09b9d-fd2e-4713-afb7-a4598e0700ba"),
            new ReserveInventoryCommand("A-500"),
            state: new DurableIncomingInboxState(
                DurableIncomingInboxStatus.Processing,
                attemptCount: 1,
                claimedBy: "stale-worker",
                claimedUntilUtc: UtcNow.AddMinutes(-5)));

        await using AsyncServiceScope seedScope = provider.CreateAsyncScope();
        IncomingInboxProcessingTestDbContext seedContext =
            seedScope.ServiceProvider.GetRequiredService<IncomingInboxProcessingTestDbContext>();
        seedContext.Set<IncomingInboxMessageEntity>().Add(
            IncomingInboxMessageEntity.FromRecord(staleRecord));
        await seedContext.SaveChangesAsync();

        await using AsyncServiceScope verificationScope = provider.CreateAsyncScope();
        IDurableIncomingInboxInspectionStore inspectionStore =
            CreateIncomingInspectionStore(verificationScope.ServiceProvider);

        DurableIncomingInboxRecord stale = Assert.Single(
            await inspectionStore.FindStaleProcessingAsync(
                UtcNow,
                receiverModule: "fulfillment",
                sourceTransportName: "rabbitmq:fulfillment.commands"));

        Assert.Equal(DurableIncomingInboxStatus.Processing, stale.State.Status);
        Assert.Equal("stale-worker", stale.State.ClaimedBy);
        Assert.Equal(UtcNow.AddMinutes(-5), stale.State.ClaimedUntilUtc);
        Assert.Equal("fulfillment", stale.ReceiverModule);
        Assert.Equal("fulfillment.reserve-inventory.v1", stale.HandlerIdentity);
    }

    private ServiceProvider CreateProvider(
        DurableIncomingInboxProcessingOptions? processingOptions = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(UtcNow));
        if (processingOptions is not null)
        {
            services.AddSingleton(processingOptions);
        }

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<IncomingInboxProcessingTestDbContext>(
                    $"{fixture.ConnectionString};Pooling=false");
                module.UseEntityFrameworkCoreDomainEventPersistence();
                module.Commands.RegisterHandler<
                    ReserveInventoryCommand,
                    ReserveInventoryResult,
                    ReserveInventoryHandler>();
                module.Commands.RegisterHandler<FailInventoryCommand, FailInventoryHandler>();
                module.Events.RegisterPublishedEvent<InventoryReservedEvent>();
            });
        });

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    private ServiceProvider CreateSharedModuleProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(UtcNow));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<IncomingInboxProcessingTestDbContext>(
                    $"{fixture.ConnectionString};Pooling=false");
                module.UseEntityFrameworkCoreDomainEventPersistence();
                module.Commands.RegisterHandler<
                    ReserveInventoryCommand,
                    ReserveInventoryResult,
                    ReserveInventoryHandler>();
                module.Events.RegisterPublishedEvent<InventoryReservedEvent>();
            });

            bondstone.Module("billing", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<IncomingInboxProcessingTestDbContext>(
                    $"{fixture.ConnectionString};Pooling=false");
                module.Commands.RegisterHandler<CapturePaymentCommand, CapturePaymentHandler>();
            });
        });

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    private static async Task ResetDatabaseAsync(IServiceProvider provider)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IncomingInboxProcessingTestDbContext context =
            scope.ServiceProvider.GetRequiredService<IncomingInboxProcessingTestDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private static async Task<DurableIncomingInboxIngestionResult> IngestAsync(
        IServiceProvider provider,
        DurableIncomingInboxRecord record)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DurableIncomingInboxIngestionBoundary boundary = scope.ServiceProvider
            .GetRequiredService<IDurableIncomingInboxIngestionBoundaryResolver>()
            .Resolve(record.ReceiverModule);

        return await boundary.IngestAndSaveAsync(record);
    }

    private static async Task<DurableIncomingInboxProcessingResult> ProcessAsync(
        IServiceProvider provider)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IDurableIncomingInboxDispatcher dispatcher =
            scope.ServiceProvider.GetRequiredService<IDurableIncomingInboxDispatcher>();

        return await dispatcher.ProcessAsync(
            "incoming-worker",
            TimeSpan.FromMinutes(5),
            maxCount: 10);
    }

    private static async Task<DurableIncomingInboxProcessingResult> ProcessModuleAsync(
        IServiceProvider provider,
        string moduleName)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DurableModulePersistenceRegistrationRegistry registry = scope.ServiceProvider
            .GetRequiredService<DurableModulePersistenceRegistrationRegistry>();
        DurableModuleIncomingInboxDispatcherRegistration registration = Assert.Single(
            registry.IncomingInboxDispatcherRegistrations,
            candidate => candidate.ModuleName == moduleName);
        IDurableIncomingInboxDispatcher dispatcher =
            registration.CreateDispatcher(scope.ServiceProvider);

        return await dispatcher.ProcessAsync(
            $"incoming-worker:{moduleName}",
            TimeSpan.FromMinutes(5),
            maxCount: 10);
    }

    private static IDurableIncomingInboxInspectionStore CreateIncomingInspectionStore(
        IServiceProvider serviceProvider)
    {
        return new EntityFrameworkCoreDurableIncomingInboxInspectionStore<IncomingInboxProcessingTestDbContext>(
            serviceProvider.GetRequiredService<IncomingInboxProcessingTestDbContext>());
    }

    private static DurableIncomingInboxRecord CreateCommandRecord<TCommand>(
        Guid messageId,
        TCommand command,
        Guid? durableOperationId = null,
        DurableIncomingInboxState? state = null)
        where TCommand : IDurableCommand
    {
        string messageTypeName = typeof(TCommand) == typeof(ReserveInventoryCommand)
            ? "fulfillment.reserve-inventory.v1"
            : "fulfillment.fail-inventory.v1";
        var serializer = new SystemTextJsonDurablePayloadSerializer();
        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Command,
            messageTypeName,
            "sales",
            "fulfillment",
            serializer.Serialize(command),
            UtcNow.AddMinutes(-1),
            durableOperationId: durableOperationId,
            partitionKey: "orders");
        var key = DurableIncomingInboxKey.ForCommandHandler(
            messageId,
            "fulfillment",
            messageTypeName);

        return new DurableIncomingInboxRecord(
            key,
            envelope,
            UtcNow.AddSeconds(-30),
            state,
            sourceTransportName: "rabbitmq:fulfillment.commands");
    }

    private static DurableIncomingInboxRecord CreateBillingCommandRecord(
        Guid messageId,
        CapturePaymentCommand command)
    {
        var serializer = new SystemTextJsonDurablePayloadSerializer();
        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Command,
            "billing.capture-payment.v1",
            "sales",
            "billing",
            serializer.Serialize(command),
            UtcNow.AddMinutes(-1),
            partitionKey: "orders");
        var key = DurableIncomingInboxKey.ForCommandHandler(
            messageId,
            "billing",
            "billing.capture-payment.v1");

        return new DurableIncomingInboxRecord(
            key,
            envelope,
            UtcNow.AddSeconds(-30),
            sourceTransportName: "rabbitmq:billing.commands");
    }

    [DurableCommandIdentity("fulfillment.reserve-inventory.v1")]
    private sealed record ReserveInventoryCommand(string OrderId)
        : IDurableCommand,
            ICommand<ReserveInventoryResult>;

    private sealed record ReserveInventoryResult(string OrderId, bool Accepted);

    private sealed class ReserveInventoryHandler(
        IncomingInboxProcessingTestDbContext context,
        IDurableEventPublisher publisher)
        : ICommandHandler<ReserveInventoryCommand, ReserveInventoryResult>
    {
        public async ValueTask<ReserveInventoryResult> HandleAsync(
            ReserveInventoryCommand command,
            CancellationToken ct = default)
        {
            context.HandledMessages.Add(new HandledMessageEntity($"reserve:{command.OrderId}"));
            context.DomainEventAggregates.Add(
                DomainEventAggregateEntity.Reserve(command.OrderId));
            await publisher.PublishAsync(
                new InventoryReservedEvent(command.OrderId),
                ct: ct);
            return new ReserveInventoryResult(command.OrderId, Accepted: true);
        }
    }

    [DurableCommandIdentity("fulfillment.fail-inventory.v1")]
    private sealed record FailInventoryCommand(string OrderId) : IDurableCommand;

    private sealed class FailInventoryHandler(
        IncomingInboxProcessingTestDbContext context,
        IDurableEventPublisher publisher)
        : ICommandHandler<FailInventoryCommand>
    {
        public async ValueTask HandleAsync(
            FailInventoryCommand command,
            CancellationToken ct = default)
        {
            context.HandledMessages.Add(new HandledMessageEntity($"fail:{command.OrderId}"));
            await publisher.PublishAsync(
                new InventoryReservedEvent(command.OrderId),
                ct: ct);
            throw new InvalidOperationException($"boom:{command.OrderId}");
        }
    }

    [DurableCommandIdentity("billing.capture-payment.v1")]
    private sealed record CapturePaymentCommand(string OrderId) : IDurableCommand;

    private sealed class CapturePaymentHandler(IncomingInboxProcessingTestDbContext context)
        : ICommandHandler<CapturePaymentCommand>
    {
        public ValueTask HandleAsync(
            CapturePaymentCommand command,
            CancellationToken ct = default)
        {
            context.HandledMessages.Add(new HandledMessageEntity($"billing:{command.OrderId}"));
            return ValueTask.CompletedTask;
        }
    }

    [IntegrationEventIdentity("fulfillment.inventory-reserved.v1")]
    private sealed record InventoryReservedEvent(string OrderId) : IIntegrationEvent;

    [DomainEventIdentity("fulfillment.inventory-reserved.domain.v1")]
    private sealed record InventoryReservedDomainEvent(string InventoryId) : IDomainEvent;

    private sealed class IncomingInboxProcessingTestDbContext(
        DbContextOptions<IncomingInboxProcessingTestDbContext> options)
        : DbContext(options)
    {
        public DbSet<HandledMessageEntity> HandledMessages => Set<HandledMessageEntity>();

        public DbSet<DomainEventAggregateEntity> DomainEventAggregates =>
            Set<DomainEventAggregateEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstonePersistence();
            modelBuilder.ApplyBondstoneDomainEvents();
            modelBuilder.Entity<HandledMessageEntity>(
                entity => entity.HasKey(record => record.Id));
            modelBuilder.Entity<DomainEventAggregateEntity>(
                entity =>
                {
                    entity.HasKey(record => record.Id);
                    entity.Ignore(record => record.PendingDomainEvents);
                    entity.Ignore(record => record.ClearCount);
                });
        }
    }

    private sealed class HandledMessageEntity(string id)
    {
        public string Id { get; set; } = id;
    }

    private sealed class DomainEventAggregateEntity : IDomainEventSource
    {
        private readonly List<IDomainEvent> _pendingDomainEvents = [];

        public string Id { get; set; } = string.Empty;

        public int ClearCount { get; private set; }

        public IReadOnlyCollection<IDomainEvent> PendingDomainEvents => _pendingDomainEvents;

        public static DomainEventAggregateEntity Reserve(
            string inventoryId)
        {
            var source = new DomainEventAggregateEntity
            {
                Id = inventoryId,
            };
            source._pendingDomainEvents.Add(new InventoryReservedDomainEvent(inventoryId));
            return source;
        }

        public void ClearPendingDomainEvents()
        {
            ClearCount++;
            _pendingDomainEvents.Clear();
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
