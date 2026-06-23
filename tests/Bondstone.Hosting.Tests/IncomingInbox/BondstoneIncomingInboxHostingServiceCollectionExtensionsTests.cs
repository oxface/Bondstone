using Bondstone.Configuration;
using Bondstone.Hosting.IncomingInbox;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Bondstone.Hosting.Tests.IncomingInbox;

public sealed class BondstoneIncomingInboxHostingServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneDurableIncomingInboxWorker_WhenServicesIsNull_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(
            () => services!.AddBondstoneDurableIncomingInboxWorker());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneDurableIncomingInboxWorker_RegistersHostedWorkerAndOptions()
    {
        var services = new ServiceCollection();

        services.AddBondstoneDurableIncomingInboxWorker();

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(DurableIncomingInboxWorker));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IValidateOptions<DurableIncomingInboxWorkerOptions>)
                && descriptor.ImplementationType == typeof(DurableIncomingInboxWorkerOptionsValidator));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(DurableIncomingInboxProcessingOptions));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneDurableIncomingInboxWorker_RegistersOnlyOneHostedWorker()
    {
        var services = new ServiceCollection();

        services.AddBondstoneDurableIncomingInboxWorker();
        services.AddBondstoneDurableIncomingInboxWorker();

        Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(DurableIncomingInboxWorker));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneDurableIncomingInboxWorker_WhenNoOptionsAreConfigured_RegistersDefaultProcessingOptions()
    {
        var services = new ServiceCollection();

        services.AddBondstoneDurableIncomingInboxWorker();

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DurableIncomingInboxProcessingOptions processingOptions =
            serviceProvider.GetRequiredService<DurableIncomingInboxProcessingOptions>();

        Assert.Equal(DurableIncomingInboxProcessingOptions.DefaultMaxAttempts, processingOptions.MaxAttempts);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseDurableIncomingInboxWorker_WhenUsedInBondstoneBuilder_RegistersWorkerAndRetryPolicyOptions()
    {
        var services = new ServiceCollection();

        services.AddBondstone(builder =>
        {
            builder.UseDurableIncomingInboxWorker(options =>
            {
                options.MaxAttempts = 7;
                options.RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DurableIncomingInboxProcessingOptions processingOptions =
            serviceProvider.GetRequiredService<DurableIncomingInboxProcessingOptions>();

        Assert.Equal(7, processingOptions.MaxAttempts);
        Assert.Equal(
            [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)],
            processingOptions.RetryDelays);
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(DurableIncomingInboxWorker));
    }
}
