using Bondstone.Persistence.EntityFrameworkCore.Inbox;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Outbox;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.Persistence;

public sealed class BondstoneEntityFrameworkCoreServiceCollectionExtensionsTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneEntityFrameworkCorePersistence_WhenServicesIsNull_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(() =>
            services!.AddBondstoneEntityFrameworkCorePersistence<EntityFrameworkCoreTestDbContext>());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneEntityFrameworkCorePersistence_RegistersDurablePersistenceServices()
    {
        var services = new ServiceCollection();

        IServiceCollection result =
            services.AddBondstoneEntityFrameworkCorePersistence<EntityFrameworkCoreTestDbContext>();

        Assert.Same(services, result);
        AssertContainsScopedFactory<IDurableOutboxWriter>(services);
        AssertContainsScoped<IDurableInboxStore, EntityFrameworkCoreDurableInboxStore<EntityFrameworkCoreTestDbContext>>(
            services);
        AssertContainsScoped<
            IDurableOperationStateStore,
            EntityFrameworkCoreDurableOperationStateStore<EntityFrameworkCoreTestDbContext>>(services);
        AssertContainsScopedFactory<IDurableOperationReader>(services);
        AssertContainsScoped<
            IEntityFrameworkCorePersistenceScope,
            EntityFrameworkCorePersistenceScope<EntityFrameworkCoreTestDbContext>>(services);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstoneEntityFrameworkCorePersistence_WhenServiceExists_DoesNotReplaceIt()
    {
        IServiceCollection services = new ServiceCollection();
        services.Add(ServiceDescriptor.Singleton<IDurableInboxStore, ExistingInboxStore>());

        services.AddBondstoneEntityFrameworkCorePersistence<EntityFrameworkCoreTestDbContext>();

        Assert.Single(services, static descriptor => descriptor.ServiceType == typeof(IDurableInboxStore));
        Assert.Contains(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(IDurableInboxStore)
                && descriptor.ImplementationType == typeof(ExistingInboxStore));
    }

    private static void AssertContainsScoped<TService, TImplementation>(
        IServiceCollection services)
    {
        Assert.Contains(
            services,
            static descriptor =>
                descriptor.ServiceType == typeof(TService)
                && descriptor.ImplementationType == typeof(TImplementation)
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

    private sealed class ExistingInboxStore : IDurableInboxStore
    {
        public ValueTask<DurableInboxRecord?> GetAsync(
            DurableInboxMessageKey key,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask AddAsync(
            DurableInboxRecord record,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask MarkProcessedAsync(
            DurableInboxMessageKey key,
            DateTimeOffset processedAtUtc,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }
}
