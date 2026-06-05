using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.EntityFrameworkCore.Postgres.Tests.Persistence;

public sealed class BondstonePostgreSqlServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstonePostgreSqlPersistence_WhenServicesIsNull_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() =>
            services!.AddBondstonePostgreSqlPersistence<PostgreSqlTestDbContext>(
                "Host=localhost;Database=bondstone"));
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("")]
    [InlineData("   ")]
    public void AddBondstonePostgreSqlPersistence_WhenConnectionStringIsBlank_Throws(
        string connectionString)
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            services.AddBondstonePostgreSqlPersistence<PostgreSqlTestDbContext>(connectionString));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstonePostgreSqlPersistence_RegistersDbContextAndBondstoneStores()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddBondstonePostgreSqlPersistence<PostgreSqlTestDbContext>(
            "Host=localhost;Database=bondstone");

        Assert.Same(services, result);
        Assert.Contains(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(DbContextOptions<PostgreSqlTestDbContext>)
                && descriptor.ImplementationFactory is not null
                && descriptor.Lifetime == ServiceLifetime.Scoped);
        AssertContainsScopedFactory<IDurableOutboxWriter>(services);
        AssertContainsScopedFactory<IDurableInboxRegistrar>(services);
        AssertContainsScopedFactory<IDurableOutboxClaimer>(services);
        AssertContainsScopedFactory<IDurableOutboxDispatchRecorder>(services);
        AssertContainsScoped<IDurableInboxStore>(services);
        AssertContainsScoped<IDurableOperationStateStore>(services);
        AssertContainsScopedFactory<IDurableOperationReader>(services);
    }

    private static void AssertContainsScoped<TService>(
        IServiceCollection services)
    {
        Assert.Contains(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(TService)
                && descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    private static void AssertContainsScopedFactory<TService>(
        IServiceCollection services)
    {
        Assert.Contains(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(TService)
                && descriptor.ImplementationFactory is not null
                && descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
