using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Samples.ModularMonolith;
using Bondstone.Samples.ModularMonolith.Ordering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

            await executor.ExecuteAsync(
                OrderingModule.ModuleName,
                new PlaceOrderCommand(
                    orderId,
                    Sku: "coffee-mug",
                    Quantity: 2,
                    DurableOperationId: durableOperationId));

            OrderStatusResult result = await WaitForResultAsync(
                serviceProvider,
                orderId,
                durableOperationId,
                TimeSpan.FromSeconds(20));

            Assert.Equal(1, result.OrderCount);
            Assert.Equal(1, result.ReservationCount);
            Assert.Equal(1, result.FulfillmentOrderEventCount);
            Assert.Equal(1, result.OrderingInventoryReservationCount);
            Assert.Equal(1, result.BillingInvoiceCount);
            Assert.Equal(4, result.ProcessedInboxCount);
            Assert.Equal(3, result.DispatchedOutboxCount);
            Assert.Equal(DurableOperationStatus.Completed, result.OperationStatus);
        }
        finally
        {
            await StopHostedServicesAsync(hostedServices);
        }
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
        Guid durableOperationId,
        TimeSpan timeout)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            OrderStatusResult result =
                await serviceProvider.ReadOrderStatusAsync(
                    orderId,
                    durableOperationId);

            if (result is
                {
                    OrderCount: 1,
                    ReservationCount: 1,
                    FulfillmentOrderEventCount: 1,
                    OrderingInventoryReservationCount: 1,
                    BillingInvoiceCount: 1,
                    ProcessedInboxCount: 4,
                    DispatchedOutboxCount: 3,
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
}
