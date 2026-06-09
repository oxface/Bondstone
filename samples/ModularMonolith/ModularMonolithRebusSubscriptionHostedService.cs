using Bondstone.Samples.ModularMonolith.Fulfillment.Contracts;
using Bondstone.Samples.ModularMonolith.Ordering.Contracts;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;

namespace Bondstone.Samples.ModularMonolith;

public sealed class ModularMonolithRebusSubscriptionHostedService(IBus bus)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await bus.Advanced.Topics.Subscribe(OrderingIntegrationEvents.OrderPlaced);
        await bus.Advanced.Topics.Subscribe(FulfillmentIntegrationEvents.InventoryReserved);
    }

    public Task StopAsync(CancellationToken ct)
    {
        return Task.CompletedTask;
    }
}
