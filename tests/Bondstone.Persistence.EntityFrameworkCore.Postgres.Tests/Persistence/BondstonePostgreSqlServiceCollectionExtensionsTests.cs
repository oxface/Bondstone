using Bondstone.Configuration;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.Persistence;

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
        AssertContainsScopedFactory<IDurableIncomingInboxClaimer>(services);
        AssertContainsScopedFactory<IDurableIncomingInboxLeaseRenewer>(services);
        AssertContainsScopedFactory<IDurableIncomingInboxOutcomeRecorder>(services);
        AssertContainsScoped<IDurableInboxStore>(services);
        AssertContainsScoped<IDurableOperationStateStore>(services);
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
        AssertContainsScopedFactory<IDurableIncomingInboxClaimer>(services);
        AssertContainsScopedFactory<IDurableIncomingInboxLeaseRenewer>(services);
        AssertContainsScopedFactory<IDurableIncomingInboxOutcomeRecorder>(services);
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
                module.Commands.RegisterHandler<TestDurableCommand, TestDurableCommandHandler>();
            });

            builder.UsePostgreSqlPersistence<PostgreSqlTestDbContext>(
                "fulfillment",
                "Host=localhost;Database=bondstone",
                schema: "fulfillment");

            persistenceWasMarked = builder.Outbox.HasPersistenceProvider;
        });

        Assert.True(persistenceWasMarked);
        AssertContainsModulePersistenceRegistrations(services, "fulfillment");
        AssertContainsTransient<DurableModuleOutboxDispatchAggregator>(services);

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        BondstoneModuleRegistration module = serviceProvider
            .GetRequiredService<IBondstoneModuleRegistry>()
            .GetModule("fulfillment");
        Assert.True(module.UsesDurableMessaging);
        Assert.Equal("EntityFrameworkCore", module.PersistenceProviderName);
        Assert.Equal(typeof(PostgreSqlTestDbContext), module.PersistenceContextType);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UsePostgreSqlPersistence_WhenUsedOnModule_RegistersModulePersistence()
    {
        var services = new ServiceCollection();
        var persistenceWasMarked = false;

        services.AddBondstone(builder =>
        {
            builder.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<PostgreSqlTestDbContext>(
                    "Host=localhost;Database=bondstone",
                    schema: "fulfillment");
                module.Commands.RegisterHandler<TestDurableCommand, TestDurableCommandHandler>();
            });

            persistenceWasMarked = builder.Outbox.HasPersistenceProvider;
        });

        Assert.True(persistenceWasMarked);
        AssertContainsModulePersistenceRegistrations(services, "fulfillment");
        AssertContainsTransient<DurableModuleOutboxDispatchAggregator>(services);
        Assert.False(
            services.Any(static descriptor => descriptor.ServiceType == typeof(IDurableOutboxWriter)),
            "Module-only PostgreSQL EF setup should not register a root IDurableOutboxWriter service.");
        Assert.False(
            services.Any(static descriptor => descriptor.ServiceType == typeof(IDurableInboxHandlerExecutor)),
            "Module-only PostgreSQL EF setup should not register a root IDurableInboxHandlerExecutor service.");
        Assert.False(
            services.Any(static descriptor => descriptor.ServiceType == typeof(IDurableOperationStateStore)),
            "Module-only PostgreSQL EF setup should not register a root IDurableOperationStateStore service.");

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        BondstoneModuleRegistration module = serviceProvider
            .GetRequiredService<IBondstoneModuleRegistry>()
            .GetModule("fulfillment");
        Assert.True(module.UsesDurableMessaging);
        Assert.Equal("EntityFrameworkCore", module.PersistenceProviderName);
        Assert.Equal(typeof(PostgreSqlTestDbContext), module.PersistenceContextType);
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

    [Fact]
    [Trait("Category", "Application")]
    public async Task DurableSend_WhenAnotherEfPostgreSqlModuleSharesHost_DoesNotResolveItsDbContext()
    {
        var writer = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        DurableModulePersistenceRegistrationRegistry registry =
            services.GetOrAddDurableModulePersistenceRegistrationRegistry();
        registry.AddOutboxWriter(new DurableModuleOutboxWriterRegistration(
            "fulfillment",
            _ => writer));
        registry.AddInboxHandlerExecutor(new DurableModuleInboxHandlerExecutorRegistration(
            "fulfillment",
            _ => new NoOpInboxHandlerExecutor()));
        registry.AddOperationStateStore(new DurableModuleOperationStateStoreRegistration(
            "fulfillment",
            _ => new NoOpOperationStateStore()));
        registry.AddOutboxDispatcher(new DurableModuleOutboxDispatcherRegistration(
            "fulfillment",
            _ => new CustomDispatcher()));
        services.AddBondstone(builder =>
        {
            builder.Module("billing", module =>
            {
                module.UseDurableMessaging();
                module.UsePostgreSqlPersistence<PostgreSqlTestDbContext>(
                    "Host=localhost;Database=bondstone",
                    schema: "billing");
            });
            builder.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<SendTestCommand, SendTestCommandHandler>();
                module.Commands.RegisterHandler<TestDurableCommand, TestDurableCommandHandler>();
            });
        });
        services.AddScoped<PostgreSqlTestDbContext>(_ =>
            throw new InvalidOperationException("PostgreSQL EF DbContext should not be resolved."));

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "fulfillment",
                new SendTestCommand());

        DurableMessageEnvelope envelope = Assert.Single(writer.Envelopes);
        Assert.Equal("fulfillment", envelope.SourceModule);
        Assert.Equal("fulfillment", envelope.TargetModule);
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

    private static void AssertContainsModulePersistenceRegistrations(
        IServiceCollection services,
        string moduleName)
    {
        DurableModulePersistenceRegistrationRegistry registry =
            services.GetOrAddDurableModulePersistenceRegistrationRegistry();

        Assert.Contains(
            registry.OutboxWriterRegistrations,
            registration => registration.ModuleName == moduleName);
        Assert.Contains(
            registry.InboxHandlerExecutorRegistrations,
            registration => registration.ModuleName == moduleName);
        Assert.Contains(
            registry.OperationStateStoreRegistrations,
            registration => registration.ModuleName == moduleName);
        Assert.Contains(
            registry.OutboxDispatcherRegistrations,
            registration => registration.ModuleName == moduleName);
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

    public sealed record SendTestCommand : ICommand;

    public sealed class TestDurableCommandHandler : ICommandHandler<TestDurableCommand>
    {
        public ValueTask HandleAsync(
            TestDurableCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    public sealed class SendTestCommandHandler(IDurableCommandSender sender)
        : ICommandHandler<SendTestCommand>
    {
        public async ValueTask HandleAsync(
            SendTestCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new TestDurableCommand(),
                "fulfillment",
                ct);
        }
    }

    private sealed class CapturingOutboxWriter : IDurableOutboxWriter
    {
        public List<DurableMessageEnvelope> Envelopes { get; } = [];

        public ValueTask WriteAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            Envelopes.Add(envelope);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class NoOpInboxHandlerExecutor : IDurableInboxHandlerExecutor
    {
        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult(new DurableInboxHandleResult(
                DurableInboxHandleStatus.AlreadyProcessed,
                record));
        }
    }

    private sealed class NoOpOperationStateStore : IDurableOperationStateStore
    {
        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult<DurableOperationState?>(null);
        }

        public ValueTask SaveAsync(
            DurableOperationState state,
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
