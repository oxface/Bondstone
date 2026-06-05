using Bondstone.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Configuration;

public sealed class BondstoneBuilderTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenServicesIsNull_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(
            () => services!.AddBondstone(_ => { }));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenConfigureIsNull_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(
            () => services.AddBondstone(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDispatcherIsConfiguredWithoutPersistence_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(builder =>
            {
                builder.Outbox.MarkTransport("test transport");
                builder.Outbox.MarkDispatcher("test dispatcher");
            }));

        Assert.Contains("persistence provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDispatcherIsConfiguredWithoutTransport_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(builder =>
            {
                builder.Outbox.MarkPersistenceProvider("test persistence");
                builder.Outbox.MarkDispatcher("test dispatcher");
            }));

        Assert.Contains("outbox transport", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDispatcherHasPersistenceAndTransport_ReturnsServices()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddBondstone(builder =>
        {
            builder.Outbox.MarkPersistenceProvider("test persistence");
            builder.Outbox.MarkTransport("test transport");
            builder.Outbox.MarkDispatcher("test dispatcher");
        });

        Assert.Same(services, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenTransportOnlyIsConfigured_AllowsManualComposition()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddBondstone(
            builder => builder.Outbox.MarkTransport("test transport"));

        Assert.Same(services, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MarkPersistenceProvider_WhenCapabilityNameIsBlank_Throws()
    {
        var services = new ServiceCollection();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => services.AddBondstone(builder =>
                builder.Outbox.MarkPersistenceProvider(" ")));

        Assert.Equal("providerName", exception.ParamName);
    }
}
