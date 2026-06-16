using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Samples.ModularMonolith;
using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Bondstone.Samples.ModularMonolith.Ordering;
using Microsoft.EntityFrameworkCore;
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
}
