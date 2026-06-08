using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Hosting.Outbox;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Bus.Advanced;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Routing;
using Rebus.Serialization.Json;
using Rebus.Transport.InMem;

namespace Bondstone.Samples.ModularMonolith;

public static class ModularMonolithSample
{
    public const string OrderingModuleName = "ordering";
    public const string FulfillmentModuleName = "fulfillment";
    public const string FulfillmentEndpointName = "fulfillment-commands";

    public static async Task<SampleRunResult> RunAsync(
        string connectionString,
        bool resetDatabase = false,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        Guid orderId = Guid.NewGuid();
        Guid durableOperationId = Guid.NewGuid();
        var rebusNetwork = new InMemNetwork();
        var routingApiBridge = new RebusRoutingApiBridge();
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            connectionString,
            routingApiBridge);

        await ResetSchemaIfRequestedAsync(serviceProvider, resetDatabase, ct);
        using var activator = new BuiltinHandlerActivator();
        activator.Handle<RebusDurableMessageEnvelope>(
            async envelope =>
            {
                await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
                RebusModuleCommandEndpointHandler handler =
                    scope.ServiceProvider.GetRequiredService<RebusModuleCommandEndpointHandler>();

                await handler.Handle(envelope);
            });

        using IBus bus = StartBus(
            activator,
            rebusNetwork,
            FulfillmentEndpointName);
        routingApiBridge.Set(bus.Advanced.Routing);

        await ExecuteOrderingCommandAsync(
            serviceProvider,
            orderId,
            durableOperationId,
            ct);

        await DispatchOutboxAsync(serviceProvider, ct);

        return await WaitForResultAsync(
            serviceProvider,
            orderId,
            durableOperationId,
            timeout ?? TimeSpan.FromSeconds(15),
            ct);
    }

    public static ServiceProvider CreateServiceProvider(
        string connectionString,
        IRoutingApi routingApi)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(routingApi);

        var services = new ServiceCollection();

        services.AddSingleton(routingApi);
        services.AddBondstoneRebusModuleCommandEndpointHandler(FulfillmentEndpointName);
        services.AddBondstone(bondstone =>
        {
            bondstone.Module(OrderingModuleName, module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<OrderingDbContext>();
                module.Commands.RegisterHandler<PlaceOrderCommand, PlaceOrderHandler>();
            });

            bondstone.Module(FulfillmentModuleName, module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<FulfillmentDbContext>();
                module.Commands.RegisterHandler<
                    ReserveInventoryCommand,
                    ReserveInventoryHandler>();
            });

            bondstone.UsePostgreSqlPersistence<OrderingDbContext>(
                OrderingModuleName,
                connectionString,
                schema: "ordering");
            bondstone.UsePostgreSqlPersistence<FulfillmentDbContext>(
                FulfillmentModuleName,
                connectionString,
                schema: "fulfillment");
            bondstone.UseRebusTransport(rebus =>
            {
                rebus
                    .UseModuleQueueConvention()
                    .ReceiveModule(FulfillmentModuleName);
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

    private static async Task ResetSchemaIfRequestedAsync(
        IServiceProvider serviceProvider,
        bool resetDatabase,
        CancellationToken ct)
    {
        if (!resetDatabase)
        {
            return;
        }

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        OrderingDbContext orderingContext =
            scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        FulfillmentDbContext fulfillmentContext =
            scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();

        await orderingContext.Database.EnsureDeletedAsync(ct);
        await orderingContext.Database.EnsureCreatedAsync(ct);
        await fulfillmentContext.Database.ExecuteSqlRawAsync(
            fulfillmentContext.Database.GenerateCreateScript(),
            ct);
    }

    private static async Task DispatchOutboxAsync(
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IDurableOutboxDispatcher dispatcher =
            scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatcher>();

        await dispatcher.DispatchAsync(
            "modular-monolith-sample",
            TimeSpan.FromMinutes(5),
            maxCount: 10,
            ct);
    }

    private static IBus StartBus(
        BuiltinHandlerActivator activator,
        InMemNetwork network,
        string inputQueueName)
    {
        return Configure
            .With(activator)
            .Transport(transport => transport.UseInMemoryTransport(network, inputQueueName))
            .Serialization(serializer => serializer.UseSystemTextJson())
            .Options(options => options.RetryStrategy("error", maxDeliveryAttempts: 1))
            .Start();
    }

    private static async Task ExecuteOrderingCommandAsync(
        IServiceProvider serviceProvider,
        Guid orderId,
        Guid durableOperationId,
        CancellationToken ct)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IModuleCommandExecutor executor =
            scope.ServiceProvider.GetRequiredService<IModuleCommandExecutor>();

        await executor.ExecuteAsync(
            OrderingModuleName,
            new PlaceOrderCommand(
                orderId,
                Sku: "coffee-mug",
                Quantity: 2,
                DurableOperationId: durableOperationId),
            ct);
    }

    private static async Task<SampleRunResult> ReadResultAsync(
        IServiceProvider serviceProvider,
        Guid orderId,
        Guid durableOperationId,
        CancellationToken ct)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        OrderingDbContext orderingContext =
            scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        FulfillmentDbContext fulfillmentContext =
            scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
        IDurableOperationReader operationReader =
            scope.ServiceProvider.GetRequiredService<IDurableOperationReader>();

        DurableOperationState? operationState =
            await operationReader.GetStateAsync(durableOperationId, ct);

        return new SampleRunResult(
            orderId,
            durableOperationId,
            await orderingContext.Orders.CountAsync(ct),
            await fulfillmentContext.Reservations.CountAsync(ct),
            await fulfillmentContext
                .Set<InboxMessageEntity>()
                .CountAsync(entity => entity.ProcessedAtUtc != null, ct),
            await orderingContext
                .Set<OutboxMessageEntity>()
                .CountAsync(entity => entity.Status == DurableOutboxStatus.Dispatched, ct),
            operationState?.Status);
    }

    private static async Task<SampleRunResult> WaitForResultAsync(
        IServiceProvider serviceProvider,
        Guid orderId,
        Guid durableOperationId,
        TimeSpan timeout,
        CancellationToken ct)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            SampleRunResult result = await ReadResultAsync(
                serviceProvider,
                orderId,
                durableOperationId,
                ct);

            if (result is
                {
                    OrderCount: 1,
                    ReservationCount: 1,
                    ProcessedInboxCount: 1,
                    DispatchedOutboxCount: 1,
                    OperationStatus: DurableOperationStatus.Completed,
                })
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25), ct);
        }

        throw new TimeoutException(
            $"Timed out waiting for durable command completion for order '{orderId}'.");
    }
}

public sealed record SampleRunResult(
    Guid OrderId,
    Guid DurableOperationId,
    int OrderCount,
    int ReservationCount,
    int ProcessedInboxCount,
    int DispatchedOutboxCount,
    DurableOperationStatus? OperationStatus);

public sealed class OrderingDbContext(
    DbContextOptions<OrderingDbContext> options)
    : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders", "ordering");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Sku).IsRequired();
        });

        modelBuilder.ApplyBondstonePersistence("ordering");
    }
}

public sealed class FulfillmentDbContext(
    DbContextOptions<FulfillmentDbContext> options)
    : DbContext(options)
{
    public DbSet<FulfillmentReservation> Reservations => Set<FulfillmentReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FulfillmentReservation>(entity =>
        {
            entity.ToTable("reservations", "fulfillment");
            entity.HasKey(reservation => reservation.Id);
            entity.Property(reservation => reservation.OrderId).IsRequired();
            entity.Property(reservation => reservation.Sku).IsRequired();
        });

        modelBuilder.ApplyBondstonePersistence("fulfillment");
    }
}

public sealed class Order
{
    public Guid Id { get; set; }

    public string Sku { get; set; } = "";

    public int Quantity { get; set; }
}

public sealed class FulfillmentReservation
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Sku { get; set; } = "";

    public int Quantity { get; set; }
}

public sealed record PlaceOrderCommand(
    Guid OrderId,
    string Sku,
    int Quantity,
    Guid DurableOperationId)
    : ICommand;

[DurableCommandIdentity("fulfillment.inventory.reserve.v1")]
public sealed record ReserveInventoryCommand(
    Guid OrderId,
    string Sku,
    int Quantity)
    : IDurableCommand;

public sealed class PlaceOrderHandler(
    OrderingDbContext dbContext,
    IDurableCommandSender commandSender)
    : ICommandHandler<PlaceOrderCommand>
{
    public async ValueTask HandleAsync(
        PlaceOrderCommand command,
        CancellationToken ct = default)
    {
        dbContext.Orders.Add(new Order
        {
            Id = command.OrderId,
            Sku = command.Sku,
            Quantity = command.Quantity,
        });

        await commandSender.SendAsync(
            new ReserveInventoryCommand(
                command.OrderId,
                command.Sku,
                command.Quantity),
            ModularMonolithSample.FulfillmentModuleName,
            partitionKey: command.OrderId.ToString("D"),
            durableOperationId: command.DurableOperationId,
            ct: ct);
    }
}

public sealed class ReserveInventoryHandler(FulfillmentDbContext dbContext)
    : ICommandHandler<ReserveInventoryCommand>
{
    public ValueTask HandleAsync(
        ReserveInventoryCommand command,
        CancellationToken ct = default)
    {
        dbContext.Reservations.Add(new FulfillmentReservation
        {
            Id = Guid.NewGuid(),
            OrderId = command.OrderId,
            Sku = command.Sku,
            Quantity = command.Quantity,
        });

        return ValueTask.CompletedTask;
    }
}

public sealed class RebusRoutingApiBridge : IRoutingApi
{
    private IRoutingApi? _routingApi;

    public void Set(IRoutingApi routingApi)
    {
        _routingApi = routingApi ?? throw new ArgumentNullException(nameof(routingApi));
    }

    public Task Send(
        string destinationAddress,
        object explicitlyRoutedMessage,
        IDictionary<string, string> optionalHeaders = null!)
    {
        return GetRoutingApi().Send(
            destinationAddress,
            explicitlyRoutedMessage,
            optionalHeaders);
    }

    public Task SendRoutingSlip(
        Itinerary itinerary,
        object message,
        IDictionary<string, string> optionalHeaders = null!)
    {
        return GetRoutingApi().SendRoutingSlip(
            itinerary,
            message,
            optionalHeaders);
    }

    public Task Defer(
        string destinationAddress,
        TimeSpan delay,
        object message,
        IDictionary<string, string> optionalHeaders = null!)
    {
        return GetRoutingApi().Defer(
            destinationAddress,
            delay,
            message,
            optionalHeaders);
    }

    private IRoutingApi GetRoutingApi()
    {
        return _routingApi
            ?? throw new InvalidOperationException("The Rebus routing API has not been attached yet.");
    }
}
