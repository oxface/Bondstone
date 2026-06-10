using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed partial class PostgreSqlPersistenceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SingleRootEntityFrameworkCorePersistence_WhenCommandSendsDurableCommand_UsesFallbackRootStores()
    {
        await ResetSingleRootFallbackDatabaseAsync();

        Guid durableOperationId = Guid.Parse("f11cc6b1-96f9-4e83-9fdc-89d335dd3fc0");
        await using ServiceProvider serviceProvider = CreateSingleRootFallbackServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "ordering",
                    new PlaceOrderCommand("order-100", durableOperationId));
        }

        await using SingleRootFallbackDbContext context = CreateSingleRootFallbackContext();
        OrderEntity order = await context.Orders.SingleAsync();
        OutboxMessageEntity outboxMessage = await context
            .Set<OutboxMessageEntity>()
            .SingleAsync();
        OperationStateEntity operationState = await context
            .Set<OperationStateEntity>()
            .SingleAsync();

        Assert.Equal("order-100", order.Id);
        Assert.Equal("ordering", outboxMessage.SourceModule);
        Assert.Equal("fulfillment", outboxMessage.TargetModule);
        Assert.Equal("fulfillment.reserve-inventory.v1", outboxMessage.MessageTypeName);
        Assert.Equal(durableOperationId, outboxMessage.DurableOperationId);
        Assert.Equal(DurableOutboxStatus.Pending, outboxMessage.Status);
        Assert.Equal(durableOperationId, operationState.DurableOperationId);
        Assert.Equal(DurableOperationStatus.Pending, operationState.Status);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task SingleRootEntityFrameworkCorePersistence_WhenCommandIsReceived_UsesFallbackRootInbox()
    {
        await ResetSingleRootFallbackDatabaseAsync();

        Guid messageId = Guid.Parse("a38f9f42-37f7-4bf8-8a78-c9d04eb7e93f");
        Guid durableOperationId = Guid.Parse("490ffbb3-8021-4d1a-a51d-fd6750da7f76");
        await using ServiceProvider serviceProvider = CreateSingleRootFallbackServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new ReserveInventoryCommand("order-100"),
                    new ModuleCommandReceiveContext(
                        new DurableInboxRecord(
                            new DurableInboxMessageKey(
                                messageId,
                                "fulfillment",
                                "fulfillment.reserve-inventory.v1"),
                            DateTimeOffset.Parse("2026-06-10T12:00:00+00:00")),
                        durableOperationId));
        }

        await using SingleRootFallbackDbContext context = CreateSingleRootFallbackContext();
        ReservationEntity reservation = await context.Reservations.SingleAsync();
        InboxMessageEntity inboxMessage = await context
            .Set<InboxMessageEntity>()
            .SingleAsync();
        OperationStateEntity operationState = await context
            .Set<OperationStateEntity>()
            .SingleAsync();

        Assert.Equal("order-100", reservation.OrderId);
        Assert.Equal(messageId, inboxMessage.MessageId);
        Assert.Equal("fulfillment", inboxMessage.ModuleName);
        Assert.Equal("fulfillment.reserve-inventory.v1", inboxMessage.HandlerIdentity);
        Assert.NotNull(inboxMessage.ProcessedAtUtc);
        Assert.Equal(durableOperationId, operationState.DurableOperationId);
        Assert.Equal(DurableOperationStatus.Completed, operationState.Status);
    }

    private ServiceProvider CreateSingleRootFallbackServiceProvider()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.UsePostgreSqlPersistence<SingleRootFallbackDbContext>(
                _fixture.ConnectionString);

            bondstone.Module("ordering", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<SingleRootFallbackDbContext>();
                module.Commands.RegisterHandler<PlaceOrderCommand, PlaceOrderHandler>();
            });

            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<SingleRootFallbackDbContext>();
                module.Commands.RegisterHandler<ReserveInventoryCommand, ReserveInventoryHandler>();
            });
        });

        return services.BuildServiceProvider();
    }

    private async Task ResetSingleRootFallbackDatabaseAsync()
    {
        await using SingleRootFallbackDbContext context = CreateSingleRootFallbackContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private SingleRootFallbackDbContext CreateSingleRootFallbackContext()
    {
        var options = new DbContextOptionsBuilder<SingleRootFallbackDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new SingleRootFallbackDbContext(options);
    }

    [DurableCommandIdentity("ordering.place-order.v1")]
    public sealed record PlaceOrderCommand(
        string OrderId,
        Guid DurableOperationId) : IDurableCommand;

    [DurableCommandIdentity("fulfillment.reserve-inventory.v1")]
    public sealed record ReserveInventoryCommand(string OrderId) : IDurableCommand;

    public sealed class PlaceOrderHandler(
        SingleRootFallbackDbContext context,
        IDurableCommandSender sender)
        : ICommandHandler<PlaceOrderCommand>
    {
        public async ValueTask HandleAsync(
            PlaceOrderCommand command,
            CancellationToken ct = default)
        {
            context.Orders.Add(new OrderEntity(command.OrderId));

            await sender.SendAsync(
                new ReserveInventoryCommand(command.OrderId),
                "fulfillment",
                partitionKey: null,
                durableOperationId: command.DurableOperationId,
                ct: ct);
        }
    }

    public sealed class ReserveInventoryHandler(SingleRootFallbackDbContext context)
        : ICommandHandler<ReserveInventoryCommand>
    {
        public ValueTask HandleAsync(
            ReserveInventoryCommand command,
            CancellationToken ct = default)
        {
            context.Reservations.Add(new ReservationEntity(command.OrderId));
            return ValueTask.CompletedTask;
        }
    }

    public sealed class SingleRootFallbackDbContext(
        DbContextOptions<SingleRootFallbackDbContext> options)
        : DbContext(options)
    {
        public DbSet<OrderEntity> Orders => Set<OrderEntity>();

        public DbSet<ReservationEntity> Reservations => Set<ReservationEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstonePersistence();
            modelBuilder.Entity<OrderEntity>(entity =>
            {
                entity.HasKey(order => order.Id);
            });
            modelBuilder.Entity<ReservationEntity>(entity =>
            {
                entity.HasKey(reservation => reservation.OrderId);
            });
        }
    }

    public sealed class OrderEntity(string id)
    {
        public string Id { get; set; } = id;
    }

    public sealed class ReservationEntity(string orderId)
    {
        public string OrderId { get; set; } = orderId;
    }
}
