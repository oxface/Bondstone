using Bondstone.Configuration;
using Bondstone.Diagnostics;
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
        Assert.False(module.UsesPersistence);
        Assert.Null(module.PersistenceProviderName);
        Assert.Null(module.PersistenceContextType);
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
                module.UsePersistence("test persistence");
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
                module.UsePersistence("test persistence");
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
    public void UsePersistence_WhenCalled_RecordsPersistenceCapability()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UsePersistence(" EntityFrameworkCore ", typeof(FulfillmentPersistenceContext));
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IBondstoneModuleRegistry registry =
            serviceProvider.GetRequiredService<IBondstoneModuleRegistry>();

        BondstoneModuleRegistration module = registry.GetModule("fulfillment");

        Assert.True(module.UsesPersistence);
        Assert.Equal("EntityFrameworkCore", module.PersistenceProviderName);
        Assert.Equal(typeof(FulfillmentPersistenceContext), module.PersistenceContextType);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UsePersistence_WhenModuleIsConfiguredMoreThanOnce_MergesCapability()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("billing", module =>
            {
                module.UsePersistence("EntityFrameworkCore");
            });
            bondstone.Module("billing", module =>
            {
                module.UsePersistence("EntityFrameworkCore", typeof(BillingPersistenceContext));
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IBondstoneModuleRegistry registry =
            serviceProvider.GetRequiredService<IBondstoneModuleRegistry>();

        BondstoneModuleRegistration module = registry.GetModule("billing");

        Assert.True(module.UsesPersistence);
        Assert.Equal("EntityFrameworkCore", module.PersistenceProviderName);
        Assert.Equal(typeof(BillingPersistenceContext), module.PersistenceContextType);
        Assert.Single(registry.Modules);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UsePersistence_WhenModuleAlreadyUsesDifferentProvider_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.ThrowsAny<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("billing", module =>
                {
                    module.UsePersistence("EntityFrameworkCore", typeof(BillingPersistenceContext));
                });
                bondstone.Module("billing", module =>
                {
                    module.UsePersistence("OtherProvider", typeof(BillingPersistenceContext));
                });
            }));

        Assert.Equal(
            BondstoneSetupCodes.DuplicateDurableRegistration,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
        Assert.Contains("already uses persistence provider", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Module 'billing'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("EntityFrameworkCore", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UsePersistence_WhenModuleAlreadyUsesDifferentContext_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.ThrowsAny<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("billing", module =>
                {
                    module.UsePersistence("EntityFrameworkCore", typeof(BillingPersistenceContext));
                });
                bondstone.Module("billing", module =>
                {
                    module.UsePersistence("EntityFrameworkCore", typeof(FulfillmentPersistenceContext));
                });
            }));

        Assert.Equal(
            BondstoneSetupCodes.DuplicateDurableRegistration,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
        Assert.Contains("already uses persistence context", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Module 'billing'", exception.Message, StringComparison.Ordinal);
        Assert.Contains(typeof(BillingPersistenceContext).FullName!, exception.Message, StringComparison.Ordinal);
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
            module.UsePersistence("test persistence");
        }
    }

    private sealed class BillingPersistenceContext;

    private sealed class FulfillmentPersistenceContext;
}
