using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Inbox;
using Bondstone.EntityFrameworkCore.Operations;
using Bondstone.EntityFrameworkCore.Outbox;
using Bondstone.Hosting.Outbox;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Samples.ModularMonolith.Fulfillment;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Bondstone.Samples.ModularMonolith.Ordering;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rebus.Config;
using Rebus.Retry.Simple;
using Rebus.Serialization.Json;
using Rebus.Transport.InMem;

namespace Bondstone.Samples.ModularMonolith;

public static class ModularMonolithApplication
{
    public static IServiceCollection AddModularMonolithSample(
        this IServiceCollection services,
        string connectionString,
        InMemNetwork rebusNetwork)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(rebusNetwork);

        services.AddLogging();
        services.AddRebus(configure => configure
            .Transport(transport => transport.UseInMemoryTransport(
                rebusNetwork,
                FulfillmentModule.CommandEndpointName))
            .Serialization(serializer => serializer.UseSystemTextJson())
            .Options(options => options.RetryStrategy("error", maxDeliveryAttempts: 1)));
        services.AddHostedService<ModularMonolithRebusSubscriptionHostedService>();
        services.AddBondstone(bondstone =>
        {
            bondstone
                .AddOrderingModule(connectionString)
                .AddFulfillmentModule(connectionString);
            bondstone.UseRebusTransport(rebus =>
            {
                rebus
                    .UseModuleQueueConvention()
                    .RouteEvent(OrderingIntegrationEvents.OrderPlaced)
                    .ToTopic(OrderingIntegrationEvents.OrderPlaced);
                rebus
                    .ReceiveEndpoint(FulfillmentModule.CommandEndpointName)
                    .AcceptModule(FulfillmentModule.ModuleName);
                rebus
                    .ReceiveEndpoint(FulfillmentModule.CommandEndpointName)
                    .SubscribeEvent(
                        OrderingIntegrationEvents.OrderPlaced,
                        FulfillmentModule.ModuleName,
                        "fulfillment.order-placed-projection.v1");
            });
            bondstone.Outbox.UseWorker(options =>
            {
                options.WorkerId = "modular-monolith-sample";
                options.BatchSize = 10;
                options.PollingInterval = TimeSpan.FromMilliseconds(25);
            });
        });

        return services;
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

        return new OrderStatusResult(
            orderId,
            durableOperationId,
            await orderingContext.Orders.CountAsync(ct),
            await fulfillmentContext.Reservations.CountAsync(ct),
            await fulfillmentContext.OrderEvents.CountAsync(ct),
            await fulfillmentContext
                .Set<InboxMessageEntity>()
                .CountAsync(entity => entity.ProcessedAtUtc != null, ct),
            await orderingContext
                .Set<OutboxMessageEntity>()
                .CountAsync(entity => entity.Status == DurableOutboxStatus.Dispatched, ct),
            operationState?.Status);
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
    int ProcessedInboxCount,
    int DispatchedOutboxCount,
    DurableOperationStatus? OperationStatus);
