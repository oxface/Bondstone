using Bondstone.Configuration;
using Bondstone.Capabilities.DomainEvents.EntityFrameworkCore.DomainEvents;
using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Hosting.Outbox;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Persistence.Postgres.Persistence;
using Bondstone.Samples.ModularMonolith.Billing;
using Bondstone.Samples.ModularMonolith.Fulfillment;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Bondstone.Samples.ModularMonolith.Ordering;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;
using Bondstone.Transport.Local.Outbox;
using Bondstone.Transport.RabbitMq.Outbox;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using RabbitMQ.Client;

namespace Bondstone.Samples.ModularMonolith;

public static class ModularMonolithApplication
{
    public static IServiceCollection AddModularMonolithSample(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddLogging();
        AddModularMonolithSampleCore(
            services,
            connectionString,
            bondstone => bondstone.UseLocalTransport(local =>
            {
                local.RouteModule(FulfillmentModule.ModuleName)
                    .ToQueue("fulfillment.commands");
                local.Queue("fulfillment.commands")
                    .AcceptModule(FulfillmentModule.ModuleName);

                local.RouteEvent(OrderingIntegrationEvents.OrderPlaced)
                    .ToQueue("ordering.order-placed");
                local.Queue("ordering.order-placed")
                    .SubscribeEvent(
                        OrderingIntegrationEvents.OrderPlaced,
                        FulfillmentModule.ModuleName,
                        "fulfillment.order-placed-projection.v1")
                    .SubscribeEvent(
                        OrderingIntegrationEvents.OrderPlaced,
                        BillingModule.ModuleName,
                        "billing.order-invoice-projection.v1");

                local.RouteEvent(FulfillmentIntegrationEvents.InventoryReserved)
                    .ToQueue("fulfillment.inventory-reserved");
                local.Queue("fulfillment.inventory-reserved")
                    .SubscribeEvent(
                        FulfillmentIntegrationEvents.InventoryReserved,
                        OrderingModule.ModuleName,
                        "ordering.inventory-reserved-projection.v1");
            }));

        return services;
    }

    public static IServiceCollection AddModularMonolithSampleWithRabbitMq(
        this IServiceCollection services,
        string connectionString,
        IConnection connection)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(connection);

        services.AddLogging();
        services.AddBondstoneRabbitMqConnection(connection);
        AddModularMonolithSampleCore(
            services,
            connectionString,
            bondstone => bondstone.UseRabbitMqTransport(rabbitMq =>
            {
                rabbitMq.UseCommandExchange("bondstone.commands");
                rabbitMq.RouteModule(FulfillmentModule.ModuleName)
                    .ToRoutingKey("fulfillment.commands");
                rabbitMq.ReceiveQueue("fulfillment.commands")
                    .AcceptModule(FulfillmentModule.ModuleName);

                rabbitMq.RouteEvent(OrderingIntegrationEvents.OrderPlaced)
                    .ToQueue("ordering.order-placed");
                rabbitMq.ReceiveQueue("ordering.order-placed")
                    .SubscribeEvent(
                        OrderingIntegrationEvents.OrderPlaced,
                        FulfillmentModule.ModuleName,
                        "fulfillment.order-placed-projection.v1")
                    .SubscribeEvent(
                        OrderingIntegrationEvents.OrderPlaced,
                        BillingModule.ModuleName,
                        "billing.order-invoice-projection.v1");

                rabbitMq.RouteEvent(FulfillmentIntegrationEvents.InventoryReserved)
                    .ToQueue("fulfillment.inventory-reserved");
                rabbitMq.ReceiveQueue("fulfillment.inventory-reserved")
                    .SubscribeEvent(
                        FulfillmentIntegrationEvents.InventoryReserved,
                        OrderingModule.ModuleName,
                        "ordering.inventory-reserved-projection.v1");

                rabbitMq.UseReceiveWorker();
            }));

        return services;
    }

    private static void AddModularMonolithSampleCore(
        IServiceCollection services,
        string connectionString,
        Action<BondstoneBuilder> configureTransport)
    {
        services.AddBondstone(bondstone =>
        {
            bondstone
                .AddOrderingModule(connectionString)
                .AddFulfillmentModule(connectionString)
                .AddBillingModule(connectionString);
            configureTransport(bondstone);
            bondstone.Outbox.UseWorker(options =>
            {
                options.WorkerId = "modular-monolith-sample";
                options.BatchSize = 10;
                options.PollingInterval = TimeSpan.FromMilliseconds(25);
            });
        });
    }

    public static IEndpointRouteBuilder MapModularMonolithSample(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(
            "/orders",
            async (
                PlaceOrderRequest? request,
                IModuleCommandExecutor executor,
                CancellationToken ct) =>
            {
                PlaceOrderRequest order = request ?? new PlaceOrderRequest(
                    Sku: "coffee-mug",
                    Quantity: 2);
                Guid orderId = Guid.NewGuid();
                Guid durableOperationId = Guid.NewGuid();

                await executor.ExecuteAsync(
                    OrderingModule.ModuleName,
                    new PlaceOrderCommand(
                        orderId,
                        order.Sku,
                        order.Quantity,
                        durableOperationId),
                    ct);

                return Results.Accepted(
                    $"/orders/{orderId}?operationId={durableOperationId}",
                    new PlaceOrderResponse(orderId, durableOperationId));
            });

        endpoints.MapGet(
            "/orders/{orderId:guid}",
            async (
                Guid orderId,
                Guid operationId,
                IServiceProvider serviceProvider,
                CancellationToken ct) =>
            {
                OrderStatusResult result =
                    await serviceProvider.ReadOrderStatusAsync(
                        orderId,
                        operationId,
                        ct);

                return Results.Ok(result);
            });

        return endpoints;
    }

    public static async Task EnsureModularMonolithDatabaseAsync(
        this IServiceProvider serviceProvider,
        bool resetDatabase = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        OrderingDbContext orderingContext =
            scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        FulfillmentDbContext fulfillmentContext =
            scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();

        if (resetDatabase)
        {
            await orderingContext.Database.EnsureDeletedAsync(ct);
        }

        await orderingContext.Database.EnsureCreatedAsync(ct);
        await fulfillmentContext.Database.ExecuteSqlRawAsync(
            fulfillmentContext.Database.GenerateCreateScript(),
            ct);
        await serviceProvider.EnsureBillingDatabaseAsync(ct);
    }

    public static async Task<OrderStatusResult> ReadOrderStatusAsync(
        this IServiceProvider serviceProvider,
        Guid orderId,
        Guid durableOperationId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        OrderingDbContext orderingContext =
            scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        FulfillmentDbContext fulfillmentContext =
            scope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
        IDurableOperationReader operationReader =
            scope.ServiceProvider.GetRequiredService<IDurableOperationReader>();

        DurableOperationState? operationState =
            await operationReader.GetStateAsync(durableOperationId, ct);
        int processedInboxCount =
            await fulfillmentContext
                .Set<InboxMessageEntity>()
                .CountAsync(entity => entity.ProcessedAtUtc != null, ct)
            + await orderingContext
                .Set<InboxMessageEntity>()
                .CountAsync(entity => entity.ProcessedAtUtc != null, ct)
            + await serviceProvider.CountBillingProcessedInboxAsync(ct);
        int dispatchedOutboxCount =
            await orderingContext
                .Set<OutboxMessageEntity>()
                .CountAsync(entity => entity.Status == DurableOutboxStatus.Dispatched, ct)
            + await fulfillmentContext
                .Set<OutboxMessageEntity>()
                .CountAsync(entity => entity.Status == DurableOutboxStatus.Dispatched, ct);
        int fulfillmentDomainEventRecordCount =
            await fulfillmentContext
                .Set<DomainEventRecordEntity>()
                .CountAsync(entity => entity.ModuleName == FulfillmentModule.ModuleName, ct);
        string? fulfillmentDomainEventName =
            await fulfillmentContext
                .Set<DomainEventRecordEntity>()
                .Where(entity => entity.ModuleName == FulfillmentModule.ModuleName)
                .OrderBy(entity => entity.CapturedAtUtc)
                .Select(entity => entity.DomainEventName)
                .SingleOrDefaultAsync(ct);

        return new OrderStatusResult(
            orderId,
            durableOperationId,
            await orderingContext.Orders.CountAsync(ct),
            await fulfillmentContext.Reservations.CountAsync(ct),
            await fulfillmentContext.OrderEvents.CountAsync(ct),
            await orderingContext.InventoryReservations.CountAsync(ct),
            await serviceProvider.CountBillingInvoicesAsync(ct),
            processedInboxCount,
            dispatchedOutboxCount,
            fulfillmentDomainEventRecordCount,
            fulfillmentDomainEventName,
            operationState?.Status);
    }

    private static async Task EnsureBillingDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        NpgsqlDataSource dataSource =
            scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

        await PostgresSchema.EnsureDurableMessagingTablesAsync(
            dataSource,
            BillingModule.ModuleName,
            ct);
        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS billing.invoices (
                "Id" uuid NOT NULL,
                "OrderId" uuid NOT NULL,
                "Sku" text NOT NULL,
                "Quantity" integer NOT NULL,
                CONSTRAINT "PK_billing_invoices" PRIMARY KEY ("Id")
            )
            """;
        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<int> CountBillingInvoicesAsync(
        this IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        NpgsqlDataSource dataSource =
            scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """SELECT COUNT(*) FROM billing.invoices""";
        object? result = await command.ExecuteScalarAsync(ct);

        return Convert.ToInt32(result, provider: null);
    }

    private static async Task<int> CountBillingProcessedInboxAsync(
        this IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        NpgsqlDataSource dataSource =
            scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        await using NpgsqlConnection connection =
            await dataSource.OpenConnectionAsync(ct);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM billing.inbox_messages
            WHERE "ProcessedAtUtc" IS NOT NULL
            """;
        object? result = await command.ExecuteScalarAsync(ct);

        return Convert.ToInt32(result, provider: null);
    }
}

public sealed record PlaceOrderRequest(
    string Sku,
    int Quantity);

public sealed record PlaceOrderResponse(
    Guid OrderId,
    Guid DurableOperationId);

public sealed record OrderStatusResult(
    Guid OrderId,
    Guid DurableOperationId,
    int OrderCount,
    int ReservationCount,
    int FulfillmentOrderEventCount,
    int OrderingInventoryReservationCount,
    int BillingInvoiceCount,
    int ProcessedInboxCount,
    int DispatchedOutboxCount,
    int FulfillmentDomainEventRecordCount,
    string? FulfillmentDomainEventName,
    DurableOperationStatus? OperationStatus);
