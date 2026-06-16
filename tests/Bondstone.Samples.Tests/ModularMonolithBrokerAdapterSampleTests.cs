using Azure.Messaging.ServiceBus;
using Bondstone.Configuration;
using Bondstone.Hosting.Outbox;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.DomainEvents;
using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Samples.ModularMonolith;
using Bondstone.Samples.ModularMonolith.Fulfillment;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Bondstone.Samples.ModularMonolith.Ordering;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;
using Bondstone.Transport.RabbitMq;
using Bondstone.Transport.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Xunit;

namespace Bondstone.Samples.Tests;

public sealed class ModularMonolithBrokerAdapterSampleTests(
    PostgreSqlSampleFixture postgres,
    RabbitMqSampleFixture rabbitMq,
    ServiceBusSampleFixture serviceBus)
    : IClassFixture<PostgreSqlSampleFixture>,
        IClassFixture<RabbitMqSampleFixture>,
        IClassFixture<ServiceBusSampleFixture>
{
    private const string RabbitMqEventExchange = "bondstone.samples.events";
    private const string RabbitMqFulfillmentCommandsQueue = "bondstone.samples.fulfillment.commands";
    private const string RabbitMqFulfillmentOrderPlacedQueue = "bondstone.samples.fulfillment.order-placed";
    private const string RabbitMqOrderingInventoryReservedQueue = "bondstone.samples.ordering.inventory-reserved";

    private const string ServiceBusFulfillmentCommandsQueue = "fulfillment-commands";
    private const string ServiceBusIntegrationEventsTopic = "integration-events";
    private const string ServiceBusFulfillmentOrderPlacedSubscription = "fulfillment-order-placed";
    private const string ServiceBusOrderingInventoryReservedSubscription = "ordering-inventory-reserved";

    private const string FulfillmentOrderPlacedSubscriber = "fulfillment.order-placed-projection.v1";
    private const string OrderingInventoryReservedSubscriber = "ordering.inventory-reserved-projection.v1";

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractedFulfillment_WhenRabbitMqAdaptersAreUsed_CompletesOperationAcrossHosts()
    {
        await using RabbitMqConnectionContext topology =
            await RabbitMqConnectionContext.OpenAsync(rabbitMq.ConnectionString);
        await DeclareRabbitMqTopologyAsync(topology.Channel);

        await using RabbitMqConnectionContext orderingRabbitMq =
            await RabbitMqConnectionContext.OpenAsync(rabbitMq.ConnectionString);
        await using RabbitMqConnectionContext fulfillmentRabbitMq =
            await RabbitMqConnectionContext.OpenAsync(rabbitMq.ConnectionString);

        var orderingServices = new ServiceCollection();
        orderingServices.AddSingleton(orderingRabbitMq.Channel);
        orderingServices.AddModularMonolithOrderingServiceSample(
            postgres.ConnectionString,
            ConfigureRabbitMqOrderingTransport);

        var fulfillmentServices = new ServiceCollection();
        fulfillmentServices.AddSingleton(fulfillmentRabbitMq.Channel);
        fulfillmentServices.AddModularMonolithFulfillmentServiceSample(
            postgres.ConnectionString,
            ConfigureRabbitMqFulfillmentTransport);

        await using ServiceProvider orderingProvider = BuildProvider(orderingServices);
        await using ServiceProvider fulfillmentProvider = BuildProvider(fulfillmentServices);

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
            ExtractedOrderStatus result = await PlaceOrderAndWaitForResultAsync(
                orderingProvider,
                fulfillmentProvider);

            AssertCompletedBrokerFlow(result);
        }
        finally
        {
            await StopHostedServicesAsync(hostedServices);
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExtractedFulfillment_WhenServiceBusAdaptersAreUsed_CompletesOperationAcrossHosts()
    {
        await using var orderingClient = new ServiceBusClient(serviceBus.ConnectionString);
        await using var fulfillmentClient = new ServiceBusClient(serviceBus.ConnectionString);

        var orderingServices = new ServiceCollection();
        orderingServices.AddSingleton(orderingClient);
        orderingServices.AddModularMonolithOrderingServiceSample(
            postgres.ConnectionString,
            ConfigureServiceBusOrderingTransport);

        var fulfillmentServices = new ServiceCollection();
        fulfillmentServices.AddSingleton(fulfillmentClient);
        fulfillmentServices.AddModularMonolithFulfillmentServiceSample(
            postgres.ConnectionString,
            ConfigureServiceBusFulfillmentTransport);

        await using ServiceProvider orderingProvider = BuildProvider(orderingServices);
        await using ServiceProvider fulfillmentProvider = BuildProvider(fulfillmentServices);

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
            ExtractedOrderStatus result = await PlaceOrderAndWaitForResultAsync(
                orderingProvider,
                fulfillmentProvider);

            AssertCompletedBrokerFlow(result);
        }
        finally
        {
            await StopHostedServicesAsync(hostedServices);
        }
    }

    private static void ConfigureRabbitMqOrderingTransport(
        BondstoneBuilder bondstone)
    {
        bondstone.UseRabbitMqDispatcher(ConfigureRabbitMqDispatcher);
        bondstone.UseRabbitMqReceiveWorker(options =>
        {
            options.QueueName = RabbitMqOrderingInventoryReservedQueue;
            options.ReceiveEvent(
                OrderingModule.ModuleName,
                OrderingInventoryReservedSubscriber);
        });
    }

    private static void ConfigureRabbitMqFulfillmentTransport(
        BondstoneBuilder bondstone)
    {
        bondstone.UseRabbitMqDispatcher(ConfigureRabbitMqDispatcher);
        bondstone.UseRabbitMqReceiveWorker(options =>
        {
            options.QueueName = RabbitMqFulfillmentCommandsQueue;
            options.ReceiveCommand();
        });
        bondstone.UseRabbitMqReceiveWorker(options =>
        {
            options.QueueName = RabbitMqFulfillmentOrderPlacedQueue;
            options.ReceiveEvent(
                FulfillmentModule.ModuleName,
                FulfillmentOrderPlacedSubscriber);
        });
    }

    private static void ConfigureRabbitMqDispatcher(
        RabbitMqEnvelopeDispatcherOptions options)
    {
        options.ResolveDestination = envelope =>
            envelope.MessageKind == MessageKind.Command
                ? new RabbitMqEnvelopeDestination(
                    string.Empty,
                    RabbitMqFulfillmentCommandsQueue)
                : new RabbitMqEnvelopeDestination(
                    RabbitMqEventExchange,
                    envelope.MessageTypeName);
    }

    private static void ConfigureServiceBusOrderingTransport(
        BondstoneBuilder bondstone)
    {
        bondstone.UseServiceBusDispatcher(ConfigureServiceBusDispatcher);
        bondstone.UseServiceBusReceiveWorker(options =>
        {
            options.TopicName = ServiceBusIntegrationEventsTopic;
            options.SubscriptionName = ServiceBusOrderingInventoryReservedSubscription;
            options.ReceiveEvent(
                OrderingModule.ModuleName,
                OrderingInventoryReservedSubscriber);
        });
    }

    private static void ConfigureServiceBusFulfillmentTransport(
        BondstoneBuilder bondstone)
    {
        bondstone.UseServiceBusDispatcher(ConfigureServiceBusDispatcher);
        bondstone.UseServiceBusReceiveWorker(options =>
        {
            options.QueueName = ServiceBusFulfillmentCommandsQueue;
            options.ReceiveCommand();
        });
        bondstone.UseServiceBusReceiveWorker(options =>
        {
            options.TopicName = ServiceBusIntegrationEventsTopic;
            options.SubscriptionName = ServiceBusFulfillmentOrderPlacedSubscription;
            options.ReceiveEvent(
                FulfillmentModule.ModuleName,
                FulfillmentOrderPlacedSubscriber);
        });
    }

    private static void ConfigureServiceBusDispatcher(
        ServiceBusEnvelopeDispatcherOptions options)
    {
        options.ResolveEntityName = envelope =>
            envelope.MessageKind == MessageKind.Command
                ? ServiceBusFulfillmentCommandsQueue
                : ServiceBusIntegrationEventsTopic;
    }

    private static async Task DeclareRabbitMqTopologyAsync(
        IChannel channel)
    {
        await channel.ExchangeDeclareAsync(
            RabbitMqEventExchange,
            ExchangeType.Topic,
            durable: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: CancellationToken.None);
        await DeclareRabbitMqQueueAsync(channel, RabbitMqFulfillmentCommandsQueue);
        await DeclareRabbitMqQueueAsync(channel, RabbitMqFulfillmentOrderPlacedQueue);
        await DeclareRabbitMqQueueAsync(channel, RabbitMqOrderingInventoryReservedQueue);
        await channel.QueueBindAsync(
            RabbitMqFulfillmentOrderPlacedQueue,
            RabbitMqEventExchange,
            OrderingIntegrationEvents.OrderPlaced,
            cancellationToken: CancellationToken.None);
        await channel.QueueBindAsync(
            RabbitMqOrderingInventoryReservedQueue,
            RabbitMqEventExchange,
            FulfillmentIntegrationEvents.InventoryReserved,
            cancellationToken: CancellationToken.None);
    }

    private static Task DeclareRabbitMqQueueAsync(
        IChannel channel,
        string queueName)
    {
        return channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: CancellationToken.None);
    }

    private static ServiceProvider BuildProvider(
        IServiceCollection services)
    {
        return services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
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

    private static async Task<ExtractedOrderStatus> PlaceOrderAndWaitForResultAsync(
        IServiceProvider orderingProvider,
        IServiceProvider fulfillmentProvider)
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

        return await WaitForExtractedResultAsync(
            orderingProvider,
            fulfillmentProvider,
            orderId,
            placeOrder.Result.ReservationOperation,
            TimeSpan.FromSeconds(30));
    }

    private static async Task<ExtractedOrderStatus> WaitForExtractedResultAsync(
        IServiceProvider orderingProvider,
        IServiceProvider fulfillmentProvider,
        Guid orderId,
        DurableOperationHandle operation,
        TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
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

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        throw new TimeoutException(
            $"Timed out waiting for broker-backed extracted fulfillment completion for order '{orderId}'.");
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

    private static void AssertCompletedBrokerFlow(
        ExtractedOrderStatus result)
    {
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
    }

    private sealed class RabbitMqConnectionContext : IAsyncDisposable
    {
        private readonly IConnection _connection;

        private RabbitMqConnectionContext(
            IConnection connection,
            IChannel channel)
        {
            _connection = connection;
            Channel = channel;
        }

        public IChannel Channel { get; }

        public static async Task<RabbitMqConnectionContext> OpenAsync(
            string connectionString)
        {
            var factory = new ConnectionFactory
            {
                Uri = new Uri(connectionString),
            };

            IConnection connection = await factory.CreateConnectionAsync(
                CancellationToken.None);
            IChannel channel = await connection.CreateChannelAsync(
                cancellationToken: CancellationToken.None);
            return new RabbitMqConnectionContext(connection, channel);
        }

        public async ValueTask DisposeAsync()
        {
            await Channel.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

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
