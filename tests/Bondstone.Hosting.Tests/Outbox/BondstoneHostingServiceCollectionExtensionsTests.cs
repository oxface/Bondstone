using Bondstone.Configuration;
using Bondstone.Hosting.Outbox;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bondstone.Hosting.Tests.Outbox;

public sealed class BondstoneHostingServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneDurableOutboxWorker_WhenServicesIsNull_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(
            () => services!.AddBondstoneDurableOutboxWorker());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneDurableOutboxWorker_RegistersHostedWorkerAndDefaultDispatcher()
    {
        var services = new ServiceCollection();

        services.AddBondstoneDurableOutboxWorker();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(DurableOutboxWorker));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IDurableOutboxDispatcher)
                && descriptor.ImplementationType == typeof(DurableOutboxDispatcher));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IValidateOptions<DurableOutboxWorkerOptions>)
                && descriptor.ImplementationType == typeof(DurableOutboxWorkerOptionsValidator));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneDurableOutboxWorker_WhenDispatcherAlreadyRegistered_DoesNotReplaceIt()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxDispatcher, CustomDispatcher>();

        services.AddBondstoneDurableOutboxWorker();

        ServiceDescriptor descriptor = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IDurableOutboxDispatcher));
        Assert.Equal(typeof(CustomDispatcher), descriptor.ImplementationType);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneDurableOutboxDispatcher_RegistersDefaultDispatcherAndPolicy()
    {
        var services = new ServiceCollection();

        services.AddBondstoneDurableOutboxDispatcher();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IDurableOutboxDispatcher)
                && descriptor.ImplementationType == typeof(DurableOutboxDispatcher));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IDurableOutboxFailurePolicy)
                && descriptor.ImplementationType == typeof(DurableOutboxFailurePolicy));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseWorker_WhenUsedInBondstoneBuilder_RegistersHostedWorkerAndMarksCapabilities()
    {
        var services = new ServiceCollection();
        var workerWasMarked = false;
        var dispatcherWasMarked = false;

        services.AddBondstone(builder =>
        {
            builder.Outbox.MarkPersistenceProvider("test persistence");
            builder.Outbox.MarkTransport("test transport");
            builder.Outbox.UseWorker();

            workerWasMarked = builder.Outbox.HasWorker;
            dispatcherWasMarked = builder.Outbox.HasDispatcher;
        });

        Assert.True(workerWasMarked);
        Assert.True(dispatcherWasMarked);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(DurableOutboxWorker));
    }

    private sealed class CustomDispatcher : IDurableOutboxDispatcher
    {
        public ValueTask<DurableOutboxDispatchResult> DispatchAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new DurableOutboxDispatchResult(0, 0, 0, 0, 0));
        }
    }
}
