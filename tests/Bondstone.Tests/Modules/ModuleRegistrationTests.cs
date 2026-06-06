using Bondstone.Configuration;
using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Modules;

public sealed class ModuleRegistrationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Module_WhenRegistered_RecordsModuleMetadata()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module(" sales ", _ => { });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IBondstoneModuleRegistry registry =
            serviceProvider.GetRequiredService<IBondstoneModuleRegistry>();

        BondstoneModuleRegistration module = registry.GetModule("sales");

        Assert.Equal("sales", module.Name);
        Assert.False(module.UsesDurableMessaging);
        Assert.Single(registry.Modules);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseDurableMessaging_WhenCalled_RecordsDurableMessagingCapability()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IBondstoneModuleRegistry registry =
            serviceProvider.GetRequiredService<IBondstoneModuleRegistry>();

        BondstoneModuleRegistration module = registry.GetModule("fulfillment");

        Assert.True(module.UsesDurableMessaging);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseDurableMessaging_WhenModuleIsConfiguredMoreThanOnce_MergesCapability()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("billing", _ => { });
            bondstone.Module("billing", module =>
            {
                module.UseDurableMessaging();
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IBondstoneModuleRegistry registry =
            serviceProvider.GetRequiredService<IBondstoneModuleRegistry>();

        BondstoneModuleRegistration module = registry.GetModule("billing");

        Assert.True(module.UsesDurableMessaging);
        Assert.Single(registry.Modules);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddModule_WhenModuleUsesDurableMessaging_RecordsDurableMessagingCapability()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.AddModule(new FulfillmentModule());
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IBondstoneModuleRegistry registry =
            serviceProvider.GetRequiredService<IBondstoneModuleRegistry>();

        BondstoneModuleRegistration module = registry.GetModule("fulfillment");

        Assert.True(module.UsesDurableMessaging);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TryGetModule_WhenModuleIsMissing_ReturnsFalse()
    {
        var services = new ServiceCollection();
        services.AddBondstone(_ => { });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IBondstoneModuleRegistry registry =
            serviceProvider.GetRequiredService<IBondstoneModuleRegistry>();

        bool found = registry.TryGetModule(
            "missing",
            out BondstoneModuleRegistration? module);

        Assert.False(found);
        Assert.Null(module);
    }

    private sealed class FulfillmentModule : IBondstoneModule
    {
        public string Name => "fulfillment";

        public void Configure(BondstoneModuleBuilder module)
        {
            module.UseDurableMessaging();
        }
    }
}
