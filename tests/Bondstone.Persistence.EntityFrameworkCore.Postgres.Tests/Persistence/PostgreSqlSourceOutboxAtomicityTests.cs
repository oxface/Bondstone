using Bondstone.Configuration;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed class PostgreSqlSourceOutboxAtomicityTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>
{
    private static readonly DateTimeOffset UtcNow =
        DateTimeOffset.Parse("2026-06-19T12:00:00+00:00");

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ModuleCommand_WhenHandlerSavesStateAndSendsDurableCommand_CommitsStateAndOutboxRow()
    {
        await using ServiceProvider provider = CreateProvider();
        await ResetDatabaseAsync(provider);

        await ExecuteAsync(
            provider,
            new PlaceOrderCommand("order-command-1"));

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        SourceOutboxAtomicityDbContext context = scope.ServiceProvider
            .GetRequiredService<SourceOutboxAtomicityDbContext>();

        SourceStateEntity state = await context.SourceStates.SingleAsync();
        OutboxMessageEntity outbox = await context.Set<OutboxMessageEntity>().SingleAsync();

        Assert.Equal("order-command-1", state.Id);
        Assert.Equal(outbox.MessageId.ToString("D"), state.OutgoingMessageId);
        Assert.Equal(MessageKind.Command, outbox.MessageKind);
        Assert.Equal("source-outbox.reserve-inventory.v1", outbox.MessageTypeName);
        Assert.Equal("ordering", outbox.SourceModule);
        Assert.Equal("fulfillment", outbox.TargetModule);
        Assert.Equal("""{"orderId":"order-command-1"}""", outbox.Payload);
        Assert.Equal(DurableOutboxStatus.Pending, outbox.Status);
        Assert.Equal(0, outbox.AttemptCount);
        Assert.Null(outbox.NextAttemptAtUtc);
        Assert.Null(outbox.DispatchedAtUtc);
        Assert.Null(outbox.FailedAtUtc);
        Assert.Null(outbox.FailureReason);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ModuleCommand_WhenHandlerSavesStateAndPublishesIntegrationEvent_CommitsStateAndEventOutboxRow()
    {
        await using ServiceProvider provider = CreateProvider();
        await ResetDatabaseAsync(provider);

        await ExecuteAsync(
            provider,
            new AcceptOrderCommand("order-event-1"));

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        SourceOutboxAtomicityDbContext context = scope.ServiceProvider
            .GetRequiredService<SourceOutboxAtomicityDbContext>();

        SourceStateEntity state = await context.SourceStates.SingleAsync();
        OutboxMessageEntity outbox = await context.Set<OutboxMessageEntity>().SingleAsync();

        Assert.Equal("order-event-1", state.Id);
        Assert.Equal(outbox.MessageId.ToString("D"), state.OutgoingMessageId);
        Assert.Equal(MessageKind.Event, outbox.MessageKind);
        Assert.Equal("source-outbox.order-accepted.v1", outbox.MessageTypeName);
        Assert.Equal("ordering", outbox.SourceModule);
        Assert.Null(outbox.TargetModule);
        Assert.Equal("""{"orderId":"order-event-1"}""", outbox.Payload);
        Assert.Equal(DurableOutboxStatus.Pending, outbox.Status);
        Assert.Equal(0, outbox.AttemptCount);
        Assert.Null(outbox.NextAttemptAtUtc);
        Assert.Null(outbox.DispatchedAtUtc);
        Assert.Null(outbox.FailedAtUtc);
        Assert.Null(outbox.FailureReason);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ModuleCommand_WhenHandlerThrowsAfterDurableSend_RollsBackStateAndOutboxRow()
    {
        await using ServiceProvider provider = CreateProvider();
        await ResetDatabaseAsync(provider);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await ExecuteAsync(
                provider,
                new FailAfterDurableSendCommand("order-rollback-1")));

        Assert.Equal("source handler failed", exception.Message);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        SourceOutboxAtomicityDbContext context = scope.ServiceProvider
            .GetRequiredService<SourceOutboxAtomicityDbContext>();

        Assert.Empty(await context.SourceStates.ToArrayAsync());
        Assert.Empty(await context.Set<OutboxMessageEntity>().ToArrayAsync());
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxWriter_WhenDuplicateMessageIdViolatesPrimaryKey_RollsBackSourceStateAndOutboxRows()
    {
        await using ServiceProvider provider = CreateProvider();
        await ResetDatabaseAsync(provider);

        Guid duplicateMessageId = Guid.Parse("33fe1dcf-1297-4f4b-a147-ff988e884d4c");
        await SeedOutboxMessageAsync(provider, duplicateMessageId);

        DbUpdateException exception = await Assert.ThrowsAsync<DbUpdateException>(
            async () => await ExecuteAsync(
                provider,
                new StageDuplicateOutboxCommand("order-duplicate-1", duplicateMessageId)));

        Assert.IsType<PostgresException>(exception.InnerException);
        Assert.Equal(PostgresErrorCodes.UniqueViolation, ((PostgresException)exception.InnerException).SqlState);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        SourceOutboxAtomicityDbContext context = scope.ServiceProvider
            .GetRequiredService<SourceOutboxAtomicityDbContext>();

        Assert.Empty(await context.SourceStates.ToArrayAsync());
        OutboxMessageEntity outbox = await context.Set<OutboxMessageEntity>().SingleAsync();
        Assert.Equal(duplicateMessageId, outbox.MessageId);
        Assert.Equal("seeded.outbox.message.v1", outbox.MessageTypeName);
        Assert.Equal("seeded", outbox.SourceModule);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatcher_WhenTerminalFailureRecorded_CanInspectTerminalOutboxEvidenceBySourceModule()
    {
        var transportFailure = new InvalidOperationException("terminal dispatch failure");
        await using ServiceProvider provider = CreateProvider(
            new ThrowingEnvelopeDispatcher(transportFailure),
            new DurableOutboxFailurePolicy(maxAttempts: 1));
        await ResetDatabaseAsync(provider);

        Guid messageId = Guid.Parse("b68d60a5-b0e4-4382-bf1f-8f6400ebf665");
        await SeedOutboxMessageAsync(
            provider,
            messageId,
            messageTypeName: "source-outbox.terminal-command.v1",
            sourceModule: "ordering",
            targetModule: "fulfillment");

        DurableOutboxDispatchResult result = await DispatchModuleOutboxAsync(provider, "ordering");

        Assert.Equal(1, result.ClaimedCount);
        Assert.Equal(0, result.DispatchedCount);
        Assert.Equal(0, result.RetryScheduledCount);
        Assert.Equal(1, result.TerminalFailedCount);
        Assert.Equal(0, result.StaleCount);

        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        SourceOutboxAtomicityDbContext context = scope.ServiceProvider
            .GetRequiredService<SourceOutboxAtomicityDbContext>();
        OutboxMessageEntity outbox = await context.Set<OutboxMessageEntity>().SingleAsync();

        Assert.Equal(DurableOutboxStatus.TerminalFailed, outbox.Status);
        Assert.Equal(1, outbox.AttemptCount);
        Assert.Equal(UtcNow, outbox.FailedAtUtc);
        Assert.Contains("terminal dispatch failure", outbox.FailureReason, StringComparison.Ordinal);

        IDurableOutboxInspector inspector = scope.ServiceProvider
            .GetRequiredService<IDurableOutboxInspector>();
        IReadOnlyList<DurableOutboxRecord> terminalFailures =
            await inspector.FindTerminalFailedAsync("ordering");

        DurableOutboxRecord failedRecord = Assert.Single(terminalFailures);
        Assert.Equal(messageId, failedRecord.Envelope.MessageId);
        Assert.Equal("ordering", failedRecord.Envelope.SourceModule);
        Assert.Equal(DurableOutboxStatus.TerminalFailed, failedRecord.DispatchState.Status);
        Assert.Equal(UtcNow, failedRecord.DispatchState.FailedAtUtc);
        Assert.Contains(
            "terminal dispatch failure",
            failedRecord.DispatchState.FailureReason,
            StringComparison.Ordinal);
    }

    private ServiceProvider CreateProvider(
        IDurableEnvelopeDispatcher? envelopeDispatcher = null,
        IDurableOutboxFailurePolicy? failurePolicy = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new FixedTimeProvider(UtcNow));
        if (envelopeDispatcher is not null)
        {
            services.AddSingleton(envelopeDispatcher);
        }

        if (failurePolicy is not null)
        {
            services.AddSingleton(failurePolicy);
        }

        services.AddBondstone(bondstone =>
        {
            bondstone.RegisterMessage<ReserveInventoryCommand>();

            bondstone.Module("ordering", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<SourceOutboxAtomicityDbContext>(
                    $"{fixture.ConnectionString};Pooling=false");
                module.Commands.RegisterHandler<PlaceOrderCommand, PlaceOrderHandler>();
                module.Commands.RegisterHandler<AcceptOrderCommand, AcceptOrderHandler>();
                module.Commands.RegisterHandler<FailAfterDurableSendCommand, FailAfterDurableSendHandler>();
                module.Commands.RegisterHandler<StageDuplicateOutboxCommand, StageDuplicateOutboxHandler>();
                module.Events.RegisterPublishedEvent<OrderAcceptedEvent>();
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
        SourceOutboxAtomicityDbContext context = scope.ServiceProvider
            .GetRequiredService<SourceOutboxAtomicityDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private static async Task ExecuteAsync<TCommand>(
        IServiceProvider provider,
        TCommand command)
        where TCommand : ICommand
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IModuleCommandExecutor executor = scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>();

        await executor.ExecuteAsync("ordering", command);
    }

    private static async Task SeedOutboxMessageAsync(
        IServiceProvider provider,
        Guid messageId,
        string messageTypeName = "seeded.outbox.message.v1",
        string sourceModule = "seeded",
        string? targetModule = "receiver")
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        SourceOutboxAtomicityDbContext context = scope.ServiceProvider
            .GetRequiredService<SourceOutboxAtomicityDbContext>();

        var writer = new EntityFrameworkCoreDurableOutboxWriter<SourceOutboxAtomicityDbContext>(
            context,
            new FixedTimeProvider(UtcNow.AddMinutes(-1)));

        await writer.WriteAsync(CreateEnvelope(
            messageId,
            messageTypeName,
            sourceModule,
            targetModule,
            payload: "{}"));
        await context.SaveChangesAsync();
    }

    private static async Task<DurableOutboxDispatchResult> DispatchModuleOutboxAsync(
        IServiceProvider provider,
        string moduleName)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        DurableModulePersistenceRegistrationRegistry registry = scope.ServiceProvider
            .GetRequiredService<DurableModulePersistenceRegistrationRegistry>();
        DurableModuleOutboxDispatcherRegistration registration = Assert.Single(
            registry.OutboxDispatcherRegistrations,
            candidate => candidate.ModuleName == moduleName);
        IDurableOutboxDispatcher dispatcher = registration.CreateDispatcher(scope.ServiceProvider);

        return await dispatcher.DispatchAsync(
            "source-outbox-worker",
            TimeSpan.FromMinutes(5),
            maxCount: 10);
    }

    private static DurableMessageEnvelope CreateEnvelope(
        Guid messageId,
        string messageTypeName,
        string sourceModule,
        string? targetModule,
        string payload)
    {
        return new DurableMessageEnvelope(
            messageId,
            targetModule is null ? MessageKind.Event : MessageKind.Command,
            messageTypeName,
            sourceModule,
            targetModule,
            payload,
            UtcNow);
    }

    private sealed record PlaceOrderCommand(string OrderId) : ICommand;

    private sealed record AcceptOrderCommand(string OrderId) : ICommand;

    private sealed record FailAfterDurableSendCommand(string OrderId) : ICommand;

    private sealed record StageDuplicateOutboxCommand(
        string OrderId,
        Guid DuplicateMessageId) : ICommand;

    [DurableCommandIdentity("source-outbox.reserve-inventory.v1")]
    private sealed record ReserveInventoryCommand(string OrderId) : IDurableCommand;

    [IntegrationEventIdentity("source-outbox.order-accepted.v1")]
    private sealed record OrderAcceptedEvent(string OrderId) : IIntegrationEvent;

    private sealed class PlaceOrderHandler(
        SourceOutboxAtomicityDbContext context,
        IDurableCommandSender sender)
        : ICommandHandler<PlaceOrderCommand>
    {
        public async ValueTask HandleAsync(
            PlaceOrderCommand command,
            CancellationToken ct = default)
        {
            DurableCommandSendResult result = await sender.SendAsync(
                new ReserveInventoryCommand(command.OrderId),
                "fulfillment",
                ct);

            context.SourceStates.Add(
                new SourceStateEntity(
                    command.OrderId,
                    result.SendId.ToString("D")));
        }
    }

    private sealed class AcceptOrderHandler(
        SourceOutboxAtomicityDbContext context,
        IDurableEventPublisher publisher)
        : ICommandHandler<AcceptOrderCommand>
    {
        public async ValueTask HandleAsync(
            AcceptOrderCommand command,
            CancellationToken ct = default)
        {
            DurableEventPublishResult result = await publisher.PublishAsync(
                new OrderAcceptedEvent(command.OrderId),
                ct);

            context.SourceStates.Add(
                new SourceStateEntity(
                    command.OrderId,
                    result.PublishId.ToString("D")));
        }
    }

    private sealed class FailAfterDurableSendHandler(
        SourceOutboxAtomicityDbContext context,
        IDurableCommandSender sender)
        : ICommandHandler<FailAfterDurableSendCommand>
    {
        public async ValueTask HandleAsync(
            FailAfterDurableSendCommand command,
            CancellationToken ct = default)
        {
            context.SourceStates.Add(
                new SourceStateEntity(
                    command.OrderId,
                    "not-committed"));

            await sender.SendAsync(
                new ReserveInventoryCommand(command.OrderId),
                "fulfillment",
                ct);

            throw new InvalidOperationException("source handler failed");
        }
    }

    private sealed class StageDuplicateOutboxHandler(
        SourceOutboxAtomicityDbContext context)
        : ICommandHandler<StageDuplicateOutboxCommand>
    {
        public async ValueTask HandleAsync(
            StageDuplicateOutboxCommand command,
            CancellationToken ct = default)
        {
            context.SourceStates.Add(
                new SourceStateEntity(
                    command.OrderId,
                    command.DuplicateMessageId.ToString("D")));

            var writer = new EntityFrameworkCoreDurableOutboxWriter<SourceOutboxAtomicityDbContext>(
                context,
                new FixedTimeProvider(UtcNow));

            await writer.WriteAsync(CreateEnvelope(
                command.DuplicateMessageId,
                "source-outbox.duplicate-command.v1",
                sourceModule: "ordering",
                targetModule: "fulfillment",
                payload: "{}"), ct);
        }
    }

    private sealed class SourceOutboxAtomicityDbContext(
        DbContextOptions<SourceOutboxAtomicityDbContext> options)
        : DbContext(options)
    {
        public DbSet<SourceStateEntity> SourceStates => Set<SourceStateEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstonePersistence();
            modelBuilder.Entity<SourceStateEntity>(
                entity =>
                {
                    entity.HasKey(source => source.Id);
                    entity.Property(source => source.OutgoingMessageId).IsRequired();
                });
        }
    }

    private sealed class SourceStateEntity(
        string id,
        string outgoingMessageId)
    {
        public string Id { get; set; } = id;

        public string OutgoingMessageId { get; set; } = outgoingMessageId;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
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
