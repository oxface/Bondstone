using Bondstone.Persistence.Postgres.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.Postgres.Tests;

public sealed class PostgresServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstonePostgresPersistence_WhenSameConnectionStringIsRegisteredTwice_DoesNotThrow()
    {
        var services = new ServiceCollection();

        services.AddBondstonePostgresPersistence("Host=localhost;Database=bondstone");
        services.AddBondstonePostgresPersistence(" Host=localhost;Database=bondstone ");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstonePostgresPersistence_WhenDifferentConnectionStringsAreRegistered_Throws()
    {
        var services = new ServiceCollection();
        services.AddBondstonePostgresPersistence("Host=localhost;Database=bondstone_a");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBondstonePostgresPersistence("Host=localhost;Database=bondstone_b"));

        Assert.Contains("one Npgsql data source", exception.Message, StringComparison.Ordinal);
    }
}
