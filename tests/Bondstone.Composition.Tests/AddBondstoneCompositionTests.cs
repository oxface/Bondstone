using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Hosting.Outbox;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rebus.Bus.Advanced;
using Rebus.Routing;
using Xunit;

namespace Bondstone.Composition.Tests;

public sealed class AddBondstoneCompositionTests
{
    [Fact]
    [Trait("Category", "Application")]
    public void AddBondstone_WithPostgreSqlRebusAndWorker_ComposesResolvableOutboxGraph()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IRoutingApi, NoOpRoutingApi>();
        services.AddSingleton<ILogger<DurableOutboxWorker>>(
            NullLogger<DurableOutboxWorker>.Instance);

        services.AddBondstone(bondstone =>
        {
            bondstone.UsePostgreSqlPersistence<CompositionDbContext>(
                "Host=localhost;Database=bondstone");
            bondstone.Outbox.UseRebusTransport(
                new Dictionary<string, string>
                {
                    ["fulfillment"] = "fulfillment-queue",
                });
            bondstone.Outbox.UseWorker(options =>
            {
                options.WorkerId = "composition-smoke-test";
                options.BatchSize = 10;
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        IHostedService hostedService = Assert.Single(
            serviceProvider.GetServices<IHostedService>());
        Assert.IsType<DurableOutboxWorker>(hostedService);

        using IServiceScope scope = serviceProvider.CreateScope();
        Assert.IsType<DurableOutboxDispatcher>(
            scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatcher>());
        Assert.IsType<RebusDurableOutboxTransport>(
            scope.ServiceProvider.GetRequiredService<IDurableOutboxTransport>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDurableOutboxClaimer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDurableOutboxLeaseRenewer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatchRecorder>());
    }

    private sealed class CompositionDbContext(DbContextOptions<CompositionDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstonePersistence();
        }
    }

    private sealed class NoOpRoutingApi : IRoutingApi
    {
        public Task Send(
            string destinationAddress,
            object explicitlyRoutedMessage,
            IDictionary<string, string> optionalHeaders = null!)
        {
            return Task.CompletedTask;
        }

        public Task SendRoutingSlip(
            Itinerary itinerary,
            object message,
            IDictionary<string, string> optionalHeaders = null!)
        {
            return Task.CompletedTask;
        }

        public Task Defer(
            string destinationAddress,
            TimeSpan delay,
            object message,
            IDictionary<string, string> optionalHeaders = null!)
        {
            return Task.CompletedTask;
        }
    }
}
