using Bondstone.Persistence.Dapper.Postgres.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.Dapper.Postgres.Tests;

public sealed class PostgresDapperServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstonePostgresDapperPersistence_WhenSameConnectionStringIsRegisteredTwice_DoesNotThrow()
    {
        var services = new ServiceCollection();

        services.AddBondstonePostgresDapperPersistence("Host=localhost;Database=bondstone");
        services.AddBondstonePostgresDapperPersistence(" Host=localhost;Database=bondstone ");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstonePostgresDapperPersistence_WhenDifferentConnectionStringsAreRegistered_Throws()
    {
        var services = new ServiceCollection();
        services.AddBondstonePostgresDapperPersistence("Host=localhost;Database=bondstone_a");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddBondstonePostgresDapperPersistence("Host=localhost;Database=bondstone_b"));

        Assert.Contains("one Npgsql data source", exception.Message, StringComparison.Ordinal);
    }
}
