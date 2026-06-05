using Bondstone.Configuration;
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
        AssertContainsScopedFactory<IDurableInboxHandlerExecutor>(services);
        AssertContainsScopedFactory<IDurableOutboxClaimer>(services);
        AssertContainsScopedFactory<IDurableOutboxLeaseRenewer>(services);
        AssertContainsScopedFactory<IDurableOutboxDispatchRecorder>(services);
        AssertContainsScoped<IDurableInboxStore>(services);
        AssertContainsScoped<IDurableOperationStateStore>(services);
        AssertContainsScopedFactory<IDurableOperationReader>(services);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UsePostgreSqlPersistence_WhenUsedInBondstoneBuilder_RegistersStoresAndMarksCapability()
    {
        var services = new ServiceCollection();
        var persistenceWasMarked = false;

        services.AddBondstone(builder =>
        {
            builder.UsePostgreSqlPersistence<PostgreSqlTestDbContext>(
                "Host=localhost;Database=bondstone");

            persistenceWasMarked = builder.Outbox.HasPersistenceProvider;
        });

        Assert.True(persistenceWasMarked);
        AssertContainsScopedFactory<IDurableOutboxClaimer>(services);
        AssertContainsScopedFactory<IDurableOutboxLeaseRenewer>(services);
        AssertContainsScopedFactory<IDurableOutboxDispatchRecorder>(services);
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
