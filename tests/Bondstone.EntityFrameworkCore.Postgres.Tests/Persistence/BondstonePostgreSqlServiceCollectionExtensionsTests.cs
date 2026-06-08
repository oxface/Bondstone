using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Modules;
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

    [Fact]
    [Trait("Category", "Unit")]
    public void UsePostgreSqlPersistence_WhenBoundToModule_RegistersModuleDurablePersistence()
    {
        var services = new ServiceCollection();
        var persistenceWasMarked = false;

        services.AddBondstone(builder =>
        {
            builder.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<PostgreSqlTestDbContext>();
                module.Commands.RegisterHandler<TestDurableCommand, TestDurableCommandHandler>();
            });

            builder.UsePostgreSqlPersistence<PostgreSqlTestDbContext>(
                "fulfillment",
                "Host=localhost;Database=bondstone",
                schema: "fulfillment");

            persistenceWasMarked = builder.Outbox.HasPersistenceProvider;
        });

        Assert.True(persistenceWasMarked);
        AssertContainsScopedFactory<IDurableModuleOutboxWriter>(services);
        AssertContainsScopedFactory<IDurableModuleInboxHandlerExecutor>(services);
        AssertContainsScopedFactory<IDurableModuleOperationStateStore>(services);
        AssertContainsScopedFactory<IDurableModuleOutboxDispatcher>(services);
        AssertContainsTransient<DurableModuleOutboxDispatchAggregator>(services);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstonePostgreSqlModulePersistence_WhenDefaultDispatcherAlreadyRegistered_ReplacesWithAggregator()
    {
        var services = new ServiceCollection();
        services.AddTransient<IDurableOutboxDispatcher, DurableOutboxDispatcher>();

        services.AddBondstonePostgreSqlModulePersistence<PostgreSqlTestDbContext>(
            "fulfillment",
            schema: "fulfillment");

        Assert.DoesNotContain(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(IDurableOutboxDispatcher)
                && descriptor.ImplementationType == typeof(DurableOutboxDispatcher));
        AssertContainsTransient<DurableModuleOutboxDispatchAggregator>(services);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstonePostgreSqlModulePersistence_WhenCustomDispatcherAlreadyRegistered_PreservesCustomDispatcher()
    {
        var services = new ServiceCollection();
        services.AddTransient<IDurableOutboxDispatcher, CustomDispatcher>();

        services.AddBondstonePostgreSqlModulePersistence<PostgreSqlTestDbContext>(
            "fulfillment",
            schema: "fulfillment");

        Assert.Contains(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(IDurableOutboxDispatcher)
                && descriptor.ImplementationType == typeof(CustomDispatcher)
                && descriptor.Lifetime == ServiceLifetime.Transient);
        Assert.DoesNotContain(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(IDurableOutboxDispatcher)
                && descriptor.ImplementationType == typeof(DurableModuleOutboxDispatchAggregator));
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

    private static void AssertContainsTransient<TImplementation>(
        IServiceCollection services)
    {
        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IDurableOutboxDispatcher)
                && descriptor.ImplementationType == typeof(TImplementation)
                && descriptor.Lifetime == ServiceLifetime.Transient);
    }

    [DurableCommandIdentity("fulfillment.test.command.v1")]
    public sealed record TestDurableCommand : IDurableCommand;

    public sealed class TestDurableCommandHandler : ICommandHandler<TestDurableCommand>
    {
        public ValueTask HandleAsync(
            TestDurableCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CustomDispatcher : IDurableOutboxDispatcher
    {
        public ValueTask<DurableOutboxDispatchResult> DispatchAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult(new DurableOutboxDispatchResult(0, 0, 0, 0, 0));
        }
    }
}
