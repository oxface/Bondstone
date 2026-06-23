using Bondstone.Configuration;
using Bondstone.Hosting.Outbox;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Transport.Local.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace Bondstone.Transport.Local.Tests;

public sealed class LocalTransportInboxPersistenceTests
    : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("bondstone_local_transport_tests")
        .Build();

    private string ConnectionString => $"{_container.GetConnectionString()};Pooling=false";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatch_WhenUsingModuleQueueConvention_PersistsConsumerInboxAndSkipsDuplicateDelivery()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await ResetDatabaseAsync(serviceProvider);

        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IModuleCommandExecutor executor =
                scope.ServiceProvider.GetRequiredService<IModuleCommandExecutor>();

            await executor.ExecuteAsync(
                "sales",
                new SubmitOrderCommand("A-100"));
        }

        OutboxMessageEntity outboxBeforeDispatch = await ReadOutboxAsync(serviceProvider);
        Assert.Equal(DurableOutboxStatus.Pending, outboxBeforeDispatch.Status);
        Assert.Empty(await ReadFulfillmentInboxAsync(serviceProvider));
        Assert.Empty(serviceProvider.GetRequiredService<FulfillmentCallLog>().Calls);

        DurableOutboxDispatchResult dispatchResult;
        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IDurableOutboxDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatcher>();

            dispatchResult = await dispatcher.DispatchAsync(
                "local-worker-1",
                TimeSpan.FromMinutes(5),
                maxCount: 10);
        }

        Assert.Equal(1, dispatchResult.ClaimedCount);
        Assert.Equal(1, dispatchResult.DispatchedCount);
        Assert.Equal(0, dispatchResult.RetryScheduledCount);
        Assert.Equal(0, dispatchResult.TerminalFailedCount);
        Assert.Equal(0, dispatchResult.StaleCount);

        OutboxMessageEntity dispatchedOutbox = await ReadOutboxAsync(serviceProvider);
        Assert.Equal(DurableOutboxStatus.Dispatched, dispatchedOutbox.Status);
        Assert.Equal("fulfillment.reserve-inventory.v1", dispatchedOutbox.MessageTypeName);
        Assert.Equal("sales", dispatchedOutbox.SourceModule);
        Assert.Equal("fulfillment", dispatchedOutbox.TargetModule);

        InboxMessageEntity persistedInbox = Assert.Single(
            await ReadFulfillmentInboxAsync(serviceProvider));
        Assert.Equal(dispatchedOutbox.MessageId, persistedInbox.MessageId);
        Assert.Equal("fulfillment", persistedInbox.ModuleName);
        Assert.Equal("fulfillment.reserve-inventory.v1", persistedInbox.HandlerIdentity);
        Assert.NotNull(persistedInbox.ProcessedAtUtc);
        Assert.Equal(["reserve:A-100"], serviceProvider.GetRequiredService<FulfillmentCallLog>().Calls);

        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IDurableEnvelopeDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<IDurableEnvelopeDispatcher>();

            await dispatcher.DispatchAsync(dispatchedOutbox.ToRecord());
        }

        Assert.Single(await ReadFulfillmentInboxAsync(serviceProvider));
        Assert.Equal(["reserve:A-100"], serviceProvider.GetRequiredService<FulfillmentCallLog>().Calls);

        DurableInboxHandleResult directResult;
        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IModuleCommandReceivePipeline pipeline =
                scope.ServiceProvider.GetRequiredService<IModuleCommandReceivePipeline>();

            directResult = await pipeline.HandleOnceAsync(dispatchedOutbox.ToRecord().Envelope);
        }

        Assert.Equal(DurableInboxHandleStatus.AlreadyProcessed, directResult.Status);
        Assert.Equal(dispatchedOutbox.MessageId, directResult.Record.Key.MessageId);
        Assert.Equal("fulfillment", directResult.Record.Key.ModuleName);
        Assert.Equal("fulfillment.reserve-inventory.v1", directResult.Record.Key.HandlerIdentity);
        Assert.Single(await ReadFulfillmentInboxAsync(serviceProvider));
        Assert.Equal(["reserve:A-100"], serviceProvider.GetRequiredService<FulfillmentCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task OutboxDispatch_WhenEventSubscribersAreConfigured_PersistsSubscriberInboxesAndSkipsDuplicateDelivery()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await ResetDatabaseAsync(serviceProvider);

        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IModuleCommandExecutor executor =
                scope.ServiceProvider.GetRequiredService<IModuleCommandExecutor>();

            await executor.ExecuteAsync(
                "sales",
                new PublishOrderSubmittedCommand("A-200"));
        }

        OutboxMessageEntity outboxBeforeDispatch = await ReadOutboxAsync(serviceProvider);
        Assert.Equal(DurableOutboxStatus.Pending, outboxBeforeDispatch.Status);
        Assert.Empty(await ReadInboxAsync(serviceProvider, "fulfillment"));
        Assert.Empty(await ReadInboxAsync(serviceProvider, "billing"));

        DurableOutboxDispatchResult dispatchResult;
        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IDurableOutboxDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatcher>();

            dispatchResult = await dispatcher.DispatchAsync(
                "local-worker-1",
                TimeSpan.FromMinutes(5),
                maxCount: 10);
        }

        Assert.Equal(1, dispatchResult.ClaimedCount);
        Assert.Equal(1, dispatchResult.DispatchedCount);
        Assert.Equal(0, dispatchResult.RetryScheduledCount);
        Assert.Equal(0, dispatchResult.TerminalFailedCount);
        Assert.Equal(0, dispatchResult.StaleCount);

        OutboxMessageEntity dispatchedOutbox = await ReadOutboxAsync(serviceProvider);
        Assert.Equal(DurableOutboxStatus.Dispatched, dispatchedOutbox.Status);
        Assert.Equal("sales.order-submitted.v1", dispatchedOutbox.MessageTypeName);
        Assert.Equal("sales", dispatchedOutbox.SourceModule);
        Assert.Null(dispatchedOutbox.TargetModule);

        InboxMessageEntity fulfillmentInbox = Assert.Single(
            await ReadInboxAsync(serviceProvider, "fulfillment"));
        Assert.Equal(dispatchedOutbox.MessageId, fulfillmentInbox.MessageId);
        Assert.Equal("fulfillment.order-submitted-projection.v1", fulfillmentInbox.HandlerIdentity);
        Assert.NotNull(fulfillmentInbox.ProcessedAtUtc);

        InboxMessageEntity billingInbox = Assert.Single(
            await ReadInboxAsync(serviceProvider, "billing"));
        Assert.Equal(dispatchedOutbox.MessageId, billingInbox.MessageId);
        Assert.Equal("billing.order-submitted-projection.v1", billingInbox.HandlerIdentity);
        Assert.NotNull(billingInbox.ProcessedAtUtc);

        Assert.Equal(
            ["order-submitted:A-200"],
            serviceProvider.GetRequiredService<FulfillmentCallLog>().Calls);
        Assert.Equal(
            ["invoice:A-200"],
            serviceProvider.GetRequiredService<BillingCallLog>().Calls);

        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IDurableEnvelopeDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<IDurableEnvelopeDispatcher>();

            await dispatcher.DispatchAsync(dispatchedOutbox.ToRecord());
        }

        Assert.Single(await ReadInboxAsync(serviceProvider, "fulfillment"));
        Assert.Single(await ReadInboxAsync(serviceProvider, "billing"));
        Assert.Equal(
            ["order-submitted:A-200"],
            serviceProvider.GetRequiredService<FulfillmentCallLog>().Calls);
        Assert.Equal(
            ["invoice:A-200"],
            serviceProvider.GetRequiredService<BillingCallLog>().Calls);

        DurableInboxHandleResult directResult;
        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IModuleEventReceivePipeline pipeline =
                scope.ServiceProvider.GetRequiredService<IModuleEventReceivePipeline>();

            directResult = await pipeline.HandleOnceAsync(
                dispatchedOutbox.ToRecord().Envelope,
                "fulfillment",
                "fulfillment.order-submitted-projection.v1");
        }

        Assert.Equal(DurableInboxHandleStatus.AlreadyProcessed, directResult.Status);
        Assert.Equal(dispatchedOutbox.MessageId, directResult.Record.Key.MessageId);
        Assert.Equal("fulfillment", directResult.Record.Key.ModuleName);
        Assert.Equal("fulfillment.order-submitted-projection.v1", directResult.Record.Key.HandlerIdentity);
        Assert.Single(await ReadInboxAsync(serviceProvider, "fulfillment"));
        Assert.Equal(
            ["order-submitted:A-200"],
            serviceProvider.GetRequiredService<FulfillmentCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DispatchAsync_WhenLocalCommandHandlerThrows_PropagatesHandlerFailure()
    {
        await using ServiceProvider serviceProvider = CreateServiceProvider();
        await ResetDatabaseAsync(serviceProvider);

        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IModuleCommandExecutor executor =
                scope.ServiceProvider.GetRequiredService<IModuleCommandExecutor>();

            await executor.ExecuteAsync(
                "sales",
                new SubmitFailingOrderCommand("A-300"));
        }

        OutboxMessageEntity outbox = await ReadOutboxAsync(serviceProvider);

        await using (AsyncServiceScope scope = serviceProvider.CreateAsyncScope())
        {
            IDurableEnvelopeDispatcher dispatcher =
                scope.ServiceProvider.GetRequiredService<IDurableEnvelopeDispatcher>();

            TestHandlerException exception = await Assert.ThrowsAsync<TestHandlerException>(
                async () => await dispatcher.DispatchAsync(outbox.ToRecord()));

            Assert.Equal("Inventory failure for order A-300.", exception.Message);
        }

        Assert.Empty(await ReadFulfillmentInboxAsync(serviceProvider));
        Assert.Equal(
            ["fail:A-300"],
            serviceProvider.GetRequiredService<FulfillmentCallLog>().Calls);
    }

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FulfillmentCallLog>();
        services.AddSingleton<BillingCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<LocalTransportTestDbContext>(ConnectionString);
                module.Commands.RegisterHandler<SubmitOrderCommand, SubmitOrderHandler>();
                module.Commands.RegisterHandler<PublishOrderSubmittedCommand, PublishOrderSubmittedHandler>();
                module.Commands.RegisterHandler<SubmitFailingOrderCommand, SubmitFailingOrderHandler>();
                module.Events.RegisterPublishedEvent<OrderSubmittedEvent>();
            });
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<LocalTransportTestDbContext>(ConnectionString);
                module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
                module.Commands.RegisterHandler<FailInventoryCommand, FailInventoryHandler>();
                module.Events.RegisterSubscriber<OrderSubmittedEvent, RecordFulfillmentOrderSubmittedHandler>(
                    "fulfillment.order-submitted-projection.v1");
            });
            bondstone.Module("billing", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<LocalTransportTestDbContext>(ConnectionString);
                module.Events.RegisterSubscriber<OrderSubmittedEvent, RecordBillingOrderSubmittedHandler>(
                    "billing.order-submitted-projection.v1");
            });
            bondstone.UseLocalTransport(local =>
            {
                local.UseModuleQueueConvention();
                local.RouteEvent("sales.order-submitted.v1").ToQueue("sales.order-submitted");
                local.Queue("sales.order-submitted")
                    .SubscribeEvent(
                        "sales.order-submitted.v1",
                        "fulfillment",
                        "fulfillment.order-submitted-projection.v1")
                    .SubscribeEvent(
                        "sales.order-submitted.v1",
                        "billing",
                        "billing.order-submitted-projection.v1");
            });
            bondstone.Outbox.UseDurableDispatcher();
        });

        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
    }

    private static async Task ResetDatabaseAsync(
        IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        LocalTransportTestDbContext context =
            scope.ServiceProvider.GetRequiredService<LocalTransportTestDbContext>();

        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private static async Task<OutboxMessageEntity> ReadOutboxAsync(
        IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        LocalTransportTestDbContext context =
            scope.ServiceProvider.GetRequiredService<LocalTransportTestDbContext>();

        return await context
            .Set<OutboxMessageEntity>()
            .SingleAsync();
    }

    private static async Task<List<InboxMessageEntity>> ReadFulfillmentInboxAsync(
        IServiceProvider serviceProvider)
    {
        return await ReadInboxAsync(serviceProvider, "fulfillment");
    }

    private static async Task<List<InboxMessageEntity>> ReadInboxAsync(
        IServiceProvider serviceProvider,
        string moduleName)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        LocalTransportTestDbContext context =
            scope.ServiceProvider.GetRequiredService<LocalTransportTestDbContext>();

        return await context
            .Set<InboxMessageEntity>()
            .Where(entity => entity.ModuleName == moduleName)
            .OrderBy(entity => entity.ReceivedAtUtc)
            .ToListAsync();
    }

    [DurableCommandIdentity("sales.submit-order.v1")]
    private sealed record SubmitOrderCommand(string OrderId) : ICommand;

    private sealed class SubmitOrderHandler(IDurableCommandSender sender)
        : ICommandHandler<SubmitOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitOrderCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new ReserveInventoryCommand(command.OrderId),
                "fulfillment",
                ct);
        }
    }

    [DurableCommandIdentity("sales.publish-order-submitted.v1")]
    private sealed record PublishOrderSubmittedCommand(string OrderId) : ICommand;

    private sealed class PublishOrderSubmittedHandler(IDurableEventPublisher publisher)
        : ICommandHandler<PublishOrderSubmittedCommand>
    {
        public async ValueTask HandleAsync(
            PublishOrderSubmittedCommand command,
            CancellationToken ct = default)
        {
            await publisher.PublishAsync(
                new OrderSubmittedEvent(command.OrderId),
                ct: ct);
        }
    }

    [DurableCommandIdentity("sales.submit-failing-order.v1")]
    private sealed record SubmitFailingOrderCommand(string OrderId) : ICommand;

    private sealed class SubmitFailingOrderHandler(IDurableCommandSender sender)
        : ICommandHandler<SubmitFailingOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitFailingOrderCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new FailInventoryCommand(command.OrderId),
                "fulfillment",
                ct);
        }
    }

    [DurableCommandIdentity("fulfillment.reserve-inventory.v1")]
    private sealed record ReserveInventoryCommand(string OrderId) : IDurableCommand;

    private sealed class ReserveInventoryHandler(FulfillmentCallLog log)
        : ICommandHandler<ReserveInventoryCommand>
    {
        public ValueTask HandleAsync(
            ReserveInventoryCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"reserve:{command.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("fulfillment.fail-inventory.v1")]
    private sealed record FailInventoryCommand(string OrderId) : IDurableCommand;

    private sealed class FailInventoryHandler(FulfillmentCallLog log)
        : ICommandHandler<FailInventoryCommand>
    {
        public ValueTask HandleAsync(
            FailInventoryCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"fail:{command.OrderId}");

            throw new TestHandlerException(
                $"Inventory failure for order {command.OrderId}.");
        }
    }

    [IntegrationEventIdentity("sales.order-submitted.v1")]
    private sealed record OrderSubmittedEvent(string OrderId) : IIntegrationEvent;

    private sealed class RecordFulfillmentOrderSubmittedHandler(FulfillmentCallLog log)
        : IIntegrationEventHandler<OrderSubmittedEvent>
    {
        public ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            CancellationToken ct = default)
        {
            log.Calls.Add($"order-submitted:{integrationEvent.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RecordBillingOrderSubmittedHandler(BillingCallLog log)
        : IIntegrationEventHandler<OrderSubmittedEvent>
    {
        public ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            CancellationToken ct = default)
        {
            log.Calls.Add($"invoice:{integrationEvent.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FulfillmentCallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class BillingCallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class TestHandlerException(string message)
        : Exception(message);

    private sealed class LocalTransportTestDbContext(
        DbContextOptions<LocalTransportTestDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstonePersistence();
        }
    }
}
