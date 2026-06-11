using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith;
using Bondstone.Samples.ModularMonolith.Ordering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Xunit;

namespace Bondstone.Samples.Tests;

public sealed class ModularMonolithRabbitMqSampleTests(
    PostgreSqlSampleFixture postgresFixture,
    RabbitMqSampleFixture rabbitMqFixture)
    : IClassFixture<PostgreSqlSampleFixture>,
        IClassFixture<RabbitMqSampleFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task AppRegistrations_WithRabbitMqTransport_CompletesDurableLoop()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        CancellationToken ct = timeout.Token;
        await using IConnection connection = await CreateConnectionAsync(ct);
        await DeclareRabbitMqTopologyAsync(connection, ct);
        var services = new ServiceCollection();
        services.AddModularMonolithSampleWithRabbitMq(
            postgresFixture.ConnectionString,
            connection);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        await serviceProvider.EnsureModularMonolithDatabaseAsync(
            resetDatabase: true,
            ct);
        IReadOnlyList<IHostedService> hostedServices =
            await StartHostedServicesAsync(serviceProvider, ct);

        try
        {
            Guid orderId = Guid.NewGuid();
            Guid durableOperationId = Guid.NewGuid();

            await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
            IModuleCommandExecutor executor =
                scope.ServiceProvider.GetRequiredService<IModuleCommandExecutor>();

            await executor.ExecuteAsync(
                OrderingModule.ModuleName,
                new PlaceOrderCommand(
                    orderId,
                    Sku: "coffee-mug",
                    Quantity: 2,
                    DurableOperationId: durableOperationId),
                ct);

            OrderStatusResult result = await WaitForResultAsync(
                serviceProvider,
                orderId,
                durableOperationId,
                TimeSpan.FromSeconds(25),
                ct);

            Assert.Equal(1, result.OrderCount);
            Assert.Equal(1, result.ReservationCount);
            Assert.Equal(1, result.FulfillmentOrderEventCount);
            Assert.Equal(1, result.OrderingInventoryReservationCount);
            Assert.Equal(1, result.BillingInvoiceCount);
            Assert.Equal(4, result.ProcessedInboxCount);
            Assert.Equal(3, result.DispatchedOutboxCount);
            Assert.Equal(1, result.FulfillmentDomainEventRecordCount);
            Assert.Equal("fulfillment.inventory-reservation-recorded.v1", result.FulfillmentDomainEventName);
            Assert.Equal(DurableOperationStatus.Completed, result.OperationStatus);
        }
        finally
        {
            await StopHostedServicesAsync(hostedServices);
        }
    }

    private async Task<IConnection> CreateConnectionAsync(
        CancellationToken ct)
    {
        var factory = new ConnectionFactory
        {
            Uri = new Uri(rabbitMqFixture.ConnectionString),
        };

        return await factory.CreateConnectionAsync(ct);
    }

    private static async Task DeclareRabbitMqTopologyAsync(
        IConnection connection,
        CancellationToken ct)
    {
        await using IChannel channel = await connection.CreateChannelAsync(
            cancellationToken: ct);
        await channel.ExchangeDeclareAsync(
            "bondstone.commands",
            ExchangeType.Direct,
            durable: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: ct);
        await DeclareQueueAsync(channel, "fulfillment.commands", ct);
        await channel.QueueBindAsync(
            "fulfillment.commands",
            "bondstone.commands",
            "fulfillment.commands",
            arguments: null,
            cancellationToken: ct);
        await DeclareQueueAsync(channel, "ordering.order-placed", ct);
        await DeclareQueueAsync(channel, "fulfillment.inventory-reserved", ct);
    }

    private static async Task DeclareQueueAsync(
        IChannel channel,
        string queueName,
        CancellationToken ct)
    {
        await channel.QueueDeclareAsync(
            queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null,
            cancellationToken: ct);
    }

    private static async Task<IReadOnlyList<IHostedService>> StartHostedServicesAsync(
        IServiceProvider serviceProvider,
        CancellationToken ct)
    {
        IHostedService[] hostedServices = serviceProvider
            .GetServices<IHostedService>()
            .ToArray();

        foreach (IHostedService hostedService in hostedServices)
        {
            await hostedService.StartAsync(ct);
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
        Guid durableOperationId,
        TimeSpan timeout,
        CancellationToken ct)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            OrderStatusResult result =
                await serviceProvider.ReadOrderStatusAsync(
                    orderId,
                    durableOperationId,
                    ct);

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
                    OperationStatus: DurableOperationStatus.Completed,
                })
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(25), ct);
        }

        throw new TimeoutException(
            $"Timed out waiting for RabbitMQ durable command completion for order '{orderId}'.");
    }
}
