using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.DomainEvents;
using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Samples.ModularMonolith;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Bondstone.Samples.ModularMonolith.Fulfillment;
using Bondstone.Samples.ModularMonolith.Ordering;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using Xunit;

namespace Bondstone.Samples.Tests;

public sealed class ModularMonolithSampleTests(PostgreSqlSampleFixture fixture)
    : IClassFixture<PostgreSqlSampleFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppRegistrations_WhenDurableCommandIsSent_ReceivesThroughModuleEndpoint()
    {
        var services = new ServiceCollection();
        services.AddModularMonolithSample(fixture.ConnectionString);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        await serviceProvider.EnsureModularMonolithDatabaseAsync(resetDatabase: true);
        IReadOnlyList<IHostedService> hostedServices =
            await StartHostedServicesAsync(serviceProvider);

        try
        {
            Guid orderId = Guid.NewGuid();
            Guid durableOperationId = Guid.NewGuid();

            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            IModuleCommandExecutor executor =
                scope.ServiceProvider.GetRequiredService<IModuleCommandExecutor>();

            ModuleCommandExecutionResult<PlaceOrderResult> placeOrder =
                await executor.ExecuteResultAsync<PlaceOrderResult>(
                    OrderingModule.ModuleName,
                    new PlaceOrderCommand(
                        orderId,
                        Sku: "coffee-mug",
                        Quantity: 2,
                        DurableOperationId: durableOperationId));
            DurableOperationHandle operation = placeOrder.Result.ReservationOperation;
            Assert.Equal(durableOperationId, operation.DurableOperationId);
            Assert.Equal(OrderingModule.ModuleName, operation.SourceModule);
            Assert.Equal(FulfillmentModule.ModuleName, operation.TargetModule);

            OrderStatusResult result = await WaitForResultAsync(
                serviceProvider,
                orderId,
                operation,
                TimeSpan.FromSeconds(20));

            Assert.Equal(1, result.OrderCount);
            Assert.Equal(1, result.ReservationCount);
            Assert.Equal(1, result.FulfillmentOrderEventCount);
            Assert.Equal(1, result.OrderingInventoryReservationCount);
            Assert.Equal(1, result.BillingInvoiceCount);
            Assert.Equal(4, result.ProcessedInboxCount);
            Assert.Equal(3, result.DispatchedOutboxCount);
            Assert.Equal(1, result.FulfillmentDomainEventRecordCount);
            Assert.Equal("fulfillment.inventory-reservation-recorded.v1", result.FulfillmentDomainEventName);
            Assert.Equal(DurableOperationResultState.CompletedWithResult, result.ReservationResultState);
            Assert.NotNull(result.ReservationId);
            Assert.Equal(DurableOperationStatus.Completed, result.OperationStatus);

            OutboxMessageEntity reserveCommandOutbox =
                await ReadFulfillmentCommandOutboxAsync(serviceProvider);
            DurableInboxHandleResult duplicateResult;
            await using (AsyncServiceScope duplicateScope = serviceProvider.CreateAsyncScope())
            {
                IModuleCommandReceivePipeline pipeline =
                    duplicateScope.ServiceProvider.GetRequiredService<IModuleCommandReceivePipeline>();

                duplicateResult = await pipeline.HandleOnceAsync(
                    reserveCommandOutbox.ToRecord().Envelope);
            }

            Assert.Equal(DurableInboxHandleStatus.AlreadyProcessed, duplicateResult.Status);
            Assert.Equal(reserveCommandOutbox.MessageId, duplicateResult.Record.Key.MessageId);
            Assert.Equal(FulfillmentModule.ModuleName, duplicateResult.Record.Key.ModuleName);
            Assert.Equal("fulfillment.inventory.reserve.v1", duplicateResult.Record.Key.HandlerIdentity);

            OrderStatusResult afterDuplicate = await serviceProvider.ReadOrderStatusAsync(
                orderId,
                operation);
            Assert.Equal(result, afterDuplicate);
        }
        finally
        {
            await StopHostedServicesAsync(hostedServices);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractedFulfillment_WhenAppOwnedBrokerBridgeIsUsed_CompletesOperationAcrossHosts()
    {
        var broker = new InMemoryEnvelopeBroker();
        var orderingServices = new ServiceCollection();
        orderingServices.AddSingleton(broker);
        orderingServices.AddModularMonolithOrderingServiceSample(
            fixture.ConnectionString,
            bondstone => bondstone.UseDurableEnvelopeDispatcher<InMemoryBrokerEnvelopeDispatcher>());

        var fulfillmentServices = new ServiceCollection();
        fulfillmentServices.AddSingleton(broker);
        fulfillmentServices.AddModularMonolithFulfillmentServiceSample(
            fixture.ConnectionString,
            bondstone => bondstone.UseDurableEnvelopeDispatcher<InMemoryBrokerEnvelopeDispatcher>());

        await using ServiceProvider orderingProvider = orderingServices.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        await using ServiceProvider fulfillmentProvider = fulfillmentServices.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        await EnsureExtractedServiceDatabasesAsync(
            orderingProvider,
            fulfillmentProvider,
            resetDatabase: true);

        List<IHostedService> hostedServices =
        [
            .. await StartHostedServicesAsync(orderingProvider),
            .. await StartHostedServicesAsync(fulfillmentProvider),
        ];

        try
        {
            Guid orderId = Guid.NewGuid();
            Guid durableOperationId = Guid.NewGuid();

            await using AsyncServiceScope scope = orderingProvider.CreateAsyncScope();
            IModuleCommandExecutor executor =
                scope.ServiceProvider.GetRequiredService<IModuleCommandExecutor>();

            ModuleCommandExecutionResult<PlaceOrderResult> placeOrder =
                await executor.ExecuteResultAsync<PlaceOrderResult>(
                    OrderingModule.ModuleName,
                    new PlaceOrderCommand(
                        orderId,
                        Sku: "coffee-mug",
                        Quantity: 2,
                        DurableOperationId: durableOperationId));
            DurableOperationHandle operation = placeOrder.Result.ReservationOperation;

            ExtractedOrderStatus result = await WaitForExtractedResultAsync(
                broker,
                orderingProvider,
                fulfillmentProvider,
                orderId,
                operation,
                TimeSpan.FromSeconds(20));

            Assert.Equal(1, result.OrderCount);
            Assert.Equal(1, result.ReservationCount);
            Assert.Equal(1, result.FulfillmentOrderEventCount);
            Assert.Equal(1, result.OrderingInventoryReservationCount);
            Assert.Equal(3, result.ProcessedInboxCount);
            Assert.Equal(3, result.DispatchedOutboxCount);
            Assert.Equal(1, result.FulfillmentDomainEventRecordCount);
            Assert.Equal("fulfillment.inventory-reservation-recorded.v1", result.FulfillmentDomainEventName);
            Assert.Equal(DurableOperationResultState.CompletedWithResult, result.ReservationResultState);
            Assert.NotNull(result.ReservationId);
            Assert.Equal(DurableOperationStatus.Pending, result.SourceOperationStatus);
            Assert.Equal(DurableOperationStatus.Completed, result.TargetOperationStatus);
            Assert.Equal(3, broker.PublishedCount);
        }
        finally
        {
            await StopHostedServicesAsync(hostedServices);
        }
    }

    private static async Task<OutboxMessageEntity> ReadFulfillmentCommandOutboxAsync(
        IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        OrderingDbContext context =
            scope.ServiceProvider.GetRequiredService<OrderingDbContext>();

        return await context
            .Set<OutboxMessageEntity>()
            .SingleAsync(entity =>
                entity.TargetModule == FulfillmentModule.ModuleName
                && entity.MessageTypeName == "fulfillment.inventory.reserve.v1");
    }

    private static async Task<IReadOnlyList<IHostedService>> StartHostedServicesAsync(
        IServiceProvider serviceProvider)
    {
        IHostedService[] hostedServices = serviceProvider
            .GetServices<IHostedService>()
            .ToArray();

        foreach (IHostedService hostedService in hostedServices)
        {
            await hostedService.StartAsync(CancellationToken.None);
        }

        return hostedServices;
    }

    private static async Task StopHostedServicesAsync(
        IReadOnlyList<IHostedService> hostedServices)
    {
        foreach (IHostedService hostedService in hostedServices.Reverse())
        {
            await hostedService.StopAsync(CancellationToken.None);
        }
    }

    private static async Task<OrderStatusResult> WaitForResultAsync(
        IServiceProvider serviceProvider,
        Guid orderId,
        DurableOperationHandle operation,
        TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            OrderStatusResult result =
                await serviceProvider.ReadOrderStatusAsync(
                    orderId,
                    operation);

            if (result is
                {
                    OrderCount: 1,
                    ReservationCount: 1,
                    FulfillmentOrderEventCount: 1,
                    OrderingInventoryReservationCount: 1,
                    BillingInvoiceCount: 1,
                    ProcessedInboxCount: 4,
                    DispatchedOutboxCount: 3,
                    FulfillmentDomainEventRecordCount: 1,
                    FulfillmentDomainEventName: "fulfillment.inventory-reservation-recorded.v1",
                    ReservationResultState: DurableOperationResultState.CompletedWithResult,
                    ReservationId: not null,
                    OperationStatus: DurableOperationStatus.Completed,
                })
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25));
        }

        throw new TimeoutException(
            $"Timed out waiting for durable command completion for order '{orderId}'.");
    }

    private static async Task EnsureExtractedServiceDatabasesAsync(
        IServiceProvider orderingProvider,
        IServiceProvider fulfillmentProvider,
        bool resetDatabase)
    {
        await using AsyncServiceScope orderingScope = orderingProvider.CreateAsyncScope();
        OrderingDbContext orderingContext =
            orderingScope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        await using AsyncServiceScope fulfillmentScope = fulfillmentProvider.CreateAsyncScope();
        FulfillmentDbContext fulfillmentContext =
            fulfillmentScope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();

        if (resetDatabase)
        {
            await orderingContext.Database.EnsureDeletedAsync();
        }

        await orderingContext.Database.EnsureCreatedAsync();
        await fulfillmentContext.Database.ExecuteSqlRawAsync(
            fulfillmentContext.Database.GenerateCreateScript());
    }

    private static async Task<ExtractedOrderStatus> WaitForExtractedResultAsync(
        InMemoryEnvelopeBroker broker,
        IServiceProvider orderingProvider,
        IServiceProvider fulfillmentProvider,
        Guid orderId,
        DurableOperationHandle operation,
        TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            await DrainBrokerAsync(
                broker,
                orderingProvider,
                fulfillmentProvider);
            ExtractedOrderStatus result = await ReadExtractedOrderStatusAsync(
                orderingProvider,
                fulfillmentProvider,
                orderId,
                operation);

            if (result is
                {
                    OrderCount: 1,
                    ReservationCount: 1,
                    FulfillmentOrderEventCount: 1,
                    OrderingInventoryReservationCount: 1,
                    ProcessedInboxCount: 3,
                    DispatchedOutboxCount: 3,
                    FulfillmentDomainEventRecordCount: 1,
                    FulfillmentDomainEventName: "fulfillment.inventory-reservation-recorded.v1",
                    ReservationResultState: DurableOperationResultState.CompletedWithResult,
                    ReservationId: not null,
                    SourceOperationStatus: DurableOperationStatus.Pending,
                    TargetOperationStatus: DurableOperationStatus.Completed,
                })
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25));
        }

        throw new TimeoutException(
            $"Timed out waiting for extracted fulfillment completion for order '{orderId}'.");
    }

    private static async Task DrainBrokerAsync(
        InMemoryEnvelopeBroker broker,
        IServiceProvider orderingProvider,
        IServiceProvider fulfillmentProvider)
    {
        IDurableMessageEnvelopeSerializer serializer = orderingProvider
            .GetRequiredService<IDurableMessageEnvelopeSerializer>();

        while (broker.TryDequeue(out InMemoryEnvelopeDelivery? delivery))
        {
            DurableMessageEnvelope envelope = serializer.Deserialize(delivery!.Body);

            if (envelope.MessageKind == MessageKind.Command)
            {
                if (envelope.TargetModule != FulfillmentModule.ModuleName)
                {
                    throw new InvalidOperationException(
                        $"Unexpected command target module '{envelope.TargetModule}'.");
                }

                await using AsyncServiceScope scope = fulfillmentProvider.CreateAsyncScope();
                IDurableEnvelopeReceiver receiver =
                    scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();
                await receiver.ReceiveCommandAsync(envelope);
                continue;
            }

            if (envelope.MessageTypeName == OrderingIntegrationEvents.OrderPlaced)
            {
                await using AsyncServiceScope scope = fulfillmentProvider.CreateAsyncScope();
                IDurableEnvelopeReceiver receiver =
                    scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();
                await receiver.ReceiveEventAsync(
                    envelope,
                    FulfillmentModule.ModuleName,
                    "fulfillment.order-placed-projection.v1");
                continue;
            }

            if (envelope.MessageTypeName == FulfillmentIntegrationEvents.InventoryReserved)
            {
                await using AsyncServiceScope scope = orderingProvider.CreateAsyncScope();
                IDurableEnvelopeReceiver receiver =
                    scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();
                await receiver.ReceiveEventAsync(
                    envelope,
                    OrderingModule.ModuleName,
                    "ordering.inventory-reserved-projection.v1");
                continue;
            }

            throw new InvalidOperationException(
                $"Unexpected integration event '{envelope.MessageTypeName}'.");
        }
    }

    private static async Task<ExtractedOrderStatus> ReadExtractedOrderStatusAsync(
        IServiceProvider orderingProvider,
        IServiceProvider fulfillmentProvider,
        Guid orderId,
        DurableOperationHandle operation)
    {
        await using AsyncServiceScope orderingScope = orderingProvider.CreateAsyncScope();
        OrderingDbContext orderingContext =
            orderingScope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        IDurableOperationReader sourceOperationReader =
            orderingScope.ServiceProvider.GetRequiredService<IDurableOperationReader>();

        await using AsyncServiceScope fulfillmentScope = fulfillmentProvider.CreateAsyncScope();
        FulfillmentDbContext fulfillmentContext =
            fulfillmentScope.ServiceProvider.GetRequiredService<FulfillmentDbContext>();
        IDurableOperationReader targetOperationReader =
            fulfillmentScope.ServiceProvider.GetRequiredService<IDurableOperationReader>();
        IDurableOperationResultReader targetOperationResultReader =
            fulfillmentScope.ServiceProvider.GetRequiredService<IDurableOperationResultReader>();

        DurableOperationState? sourceOperationState =
            await sourceOperationReader.GetStateAsync(
                operation.DurableOperationId,
                OrderingModule.ModuleName);
        DurableOperationState? targetOperationState =
            await targetOperationReader.GetStateAsync(operation);
        DurableOperationResult<ReserveInventoryResult> operationResult =
            await targetOperationResultReader.GetResultAsync<ReserveInventoryResult>(
                operation);
        int processedInboxCount =
            await fulfillmentContext
                .Set<InboxMessageEntity>()
                .CountAsync(entity => entity.ProcessedAtUtc != null)
            + await orderingContext
                .Set<InboxMessageEntity>()
                .CountAsync(entity => entity.ProcessedAtUtc != null);
        int dispatchedOutboxCount =
            await orderingContext
                .Set<OutboxMessageEntity>()
                .CountAsync(entity => entity.Status == DurableOutboxStatus.Dispatched)
            + await fulfillmentContext
                .Set<OutboxMessageEntity>()
                .CountAsync(entity => entity.Status == DurableOutboxStatus.Dispatched);
        int fulfillmentDomainEventRecordCount =
            await fulfillmentContext
                .Set<DomainEventRecordEntity>()
                .CountAsync(entity => entity.ModuleName == FulfillmentModule.ModuleName);
        string? fulfillmentDomainEventName =
            await fulfillmentContext
                .Set<DomainEventRecordEntity>()
                .Where(entity => entity.ModuleName == FulfillmentModule.ModuleName)
                .OrderBy(entity => entity.CapturedAtUtc)
                .Select(entity => entity.DomainEventName)
                .SingleOrDefaultAsync();

        return new ExtractedOrderStatus(
            orderId,
            operation,
            await orderingContext.Orders.CountAsync(),
            await fulfillmentContext.Reservations.CountAsync(),
            await fulfillmentContext.OrderEvents.CountAsync(),
            await orderingContext.InventoryReservations.CountAsync(),
            processedInboxCount,
            dispatchedOutboxCount,
            fulfillmentDomainEventRecordCount,
            fulfillmentDomainEventName,
            operationResult.State,
            operationResult.Result?.ReservationId,
            sourceOperationState?.Status,
            targetOperationState?.Status);
    }

    private sealed class InMemoryBrokerEnvelopeDispatcher(
        InMemoryEnvelopeBroker broker,
        IDurableMessageEnvelopeSerializer serializer)
        : IDurableEnvelopeDispatcher
    {
        public ValueTask DispatchAsync(
            DurableOutboxRecord record,
            CancellationToken ct = default)
        {
            broker.Publish(serializer.SerializeToUtf8Bytes(record.Envelope));
            return ValueTask.CompletedTask;
        }
    }

    private sealed class InMemoryEnvelopeBroker
    {
        private readonly ConcurrentQueue<InMemoryEnvelopeDelivery> _deliveries = new();

        public void Publish(byte[] body)
        {
            _deliveries.Enqueue(new InMemoryEnvelopeDelivery(body));
            Interlocked.Increment(ref _publishedCount);
        }

        private int _publishedCount;

        public int PublishedCount => _publishedCount;

        public bool TryDequeue(out InMemoryEnvelopeDelivery? delivery)
        {
            return _deliveries.TryDequeue(out delivery);
        }
    }

    private sealed record InMemoryEnvelopeDelivery(byte[] Body);

    private sealed record ExtractedOrderStatus(
        Guid OrderId,
        DurableOperationHandle Operation,
        int OrderCount,
        int ReservationCount,
        int FulfillmentOrderEventCount,
        int OrderingInventoryReservationCount,
        int ProcessedInboxCount,
        int DispatchedOutboxCount,
        int FulfillmentDomainEventRecordCount,
        string? FulfillmentDomainEventName,
        DurableOperationResultState ReservationResultState,
        Guid? ReservationId,
        DurableOperationStatus? SourceOperationStatus,
        DurableOperationStatus? TargetOperationStatus);
}
