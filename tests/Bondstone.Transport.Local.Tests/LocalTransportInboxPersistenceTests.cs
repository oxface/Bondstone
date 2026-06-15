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
            IDurableOutboxTransport transport =
                scope.ServiceProvider.GetRequiredService<IDurableOutboxTransport>();

            await transport.SendAsync(dispatchedOutbox.ToRecord());
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

    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FulfillmentCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<LocalTransportTestDbContext>(ConnectionString);
                module.Commands.RegisterHandler<SubmitOrderCommand, SubmitOrderHandler>();
            });
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<LocalTransportTestDbContext>(ConnectionString);
                module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
            });
            bondstone.UseLocalTransport(local => local.UseModuleQueueConvention());
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
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        LocalTransportTestDbContext context =
            scope.ServiceProvider.GetRequiredService<LocalTransportTestDbContext>();

        return await context
            .Set<InboxMessageEntity>()
            .Where(entity => entity.ModuleName == "fulfillment")
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

    private sealed class FulfillmentCallLog
    {
        public List<string> Calls { get; } = [];
    }

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
