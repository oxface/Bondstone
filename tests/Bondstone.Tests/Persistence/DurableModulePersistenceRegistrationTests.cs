using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class DurableModulePersistenceRegistrationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void CommandSender_WhenDuplicateModuleOutboxWritersAreRegistered_ThrowsClearError()
    {
        var services = new ServiceCollection();
        RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
            "sales",
            _ => new TestModuleOutboxWriter("sales")));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
                " sales ",
                _ => new TestModuleOutboxWriter(" sales "))));

        Assert.Contains("durable module outbox writer", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommandPipelineBehavior_WhenDuplicateModuleInboxExecutorsAreRegistered_ThrowsClearError()
    {
        var services = new ServiceCollection();
        RegisterInboxHandlerExecutor(services, new DurableModuleInboxHandlerExecutorRegistration(
            "fulfillment",
            _ => new TestModuleInboxHandlerExecutor("fulfillment")));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RegisterInboxHandlerExecutor(services, new DurableModuleInboxHandlerExecutorRegistration(
                " fulfillment ",
                _ => new TestModuleInboxHandlerExecutor(" fulfillment "))));

        Assert.Contains("durable module inbox handler executor", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void InboxInspector_WhenDuplicateModuleInspectionStoresAreRegistered_ThrowsClearError()
    {
        var services = new ServiceCollection();
        RegisterInboxInspectionStore(services, new DurableModuleInboxInspectionStoreRegistration(
            "fulfillment",
            _ => new TestModuleInboxInspectionStore()));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RegisterInboxInspectionStore(services, new DurableModuleInboxInspectionStoreRegistration(
                " fulfillment ",
                _ => new TestModuleInboxInspectionStore())));

        Assert.Contains("durable module inbox inspection store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OperationReader_WhenDuplicateModuleOperationStoresAreRegistered_ThrowsClearError()
    {
        var services = new ServiceCollection();
        RegisterOperationStateStore(services, new DurableModuleOperationStateStoreRegistration(
            "sales",
            _ => new TestModuleOperationStateStore("sales")));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RegisterOperationStateStore(services, new DurableModuleOperationStateStoreRegistration(
                " sales ",
                _ => new TestModuleOperationStateStore(" sales "))));

        Assert.Contains("durable module operation-state store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OutboxDispatchAggregator_WhenDuplicateModuleDispatchersAreRegistered_ThrowsClearError()
    {
        var services = new ServiceCollection();
        RegisterOutboxDispatcher(services, new DurableModuleOutboxDispatcherRegistration(
            "sales",
            _ => new TestModuleOutboxDispatcher()));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RegisterOutboxDispatcher(services, new DurableModuleOutboxDispatcherRegistration(
                " sales ",
                _ => new TestModuleOutboxDispatcher())));

        Assert.Contains("durable module outbox dispatcher", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OutboxInspector_WhenDuplicateModuleInspectionStoresAreRegistered_ThrowsClearError()
    {
        var services = new ServiceCollection();
        RegisterOutboxInspectionStore(services, new DurableModuleOutboxInspectionStoreRegistration(
            "sales",
            _ => new TestModuleOutboxInspectionStore()));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => RegisterOutboxInspectionStore(services, new DurableModuleOutboxInspectionStoreRegistration(
                " sales ",
                _ => new TestModuleOutboxInspectionStore())));

        Assert.Contains("durable module outbox inspection store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CommandSender_WhenModuleOutboxWriterIsMissing_ThrowsProviderSpecificError()
    {
        var services = new ServiceCollection();
        services.AddBondstone(bondstone =>
        {
            RegisterSalesSendingCommand(bondstone);
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "sales",
                    new TestSendingCommand()));

        Assert.Contains("durable module outbox writer", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("test persistence", exception.Message, StringComparison.Ordinal);
        Assert.Contains("module persistence services", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommandSender_WhenOnlyAnotherModuleOutboxWriterExists_FailsAtStartup()
    {
        var fallbackWriter = new CapturingOutboxWriter();
        var fulfillmentWriter = new CapturingModuleOutboxWriter("fulfillment");
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(fallbackWriter);
        RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
            fulfillmentWriter.ModuleName,
            _ => fulfillmentWriter));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                RegisterSalesSendingCommand(bondstone);
            }));

        Assert.Contains("missing durable module persistence role registrations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fallbackWriter.Envelopes);
        Assert.Empty(fulfillmentWriter.Envelopes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommandSender_WhenRootWriterExistsButModuleRuntimeIsPartial_FailsAtStartup()
    {
        var fallbackWriter = new CapturingOutboxWriter();
        var salesInboxExecutor = new CapturingModuleInboxHandlerExecutor("sales");
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(fallbackWriter);
        RegisterInboxHandlerExecutor(services, new DurableModuleInboxHandlerExecutorRegistration(
            salesInboxExecutor.ModuleName,
            _ => salesInboxExecutor));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                RegisterSalesSendingCommand(bondstone);
            }));

        Assert.Contains("missing durable module persistence role registrations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Empty(fallbackWriter.Envelopes);
        Assert.Equal(0, salesInboxExecutor.HandleCalls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CommandReceive_WhenModuleInboxExecutorIsMissing_ThrowsProviderSpecificError()
    {
        var services = new ServiceCollection();
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandReceivePipeline>()
                .HandleOnceAsync(CreateTestCommandEnvelope()));

        Assert.Contains("durable module inbox handler executor", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("test persistence", exception.Message, StringComparison.Ordinal);
        Assert.Contains("module persistence services", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommandReceive_WhenOnlyAnotherModuleInboxExecutorExists_FailsAtStartup()
    {
        var fallbackExecutor = new CapturingInboxHandlerExecutor();
        var billingExecutor = new CapturingModuleInboxHandlerExecutor("billing");
        var services = new ServiceCollection();
        services.AddSingleton<IDurableInboxHandlerExecutor>(fallbackExecutor);
        RegisterInboxHandlerExecutor(services, new DurableModuleInboxHandlerExecutorRegistration(
            billingExecutor.ModuleName,
            _ => billingExecutor));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                    module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
                });
                bondstone.Module("billing", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                });
            }));

        Assert.Contains("missing durable module persistence role registrations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, fallbackExecutor.HandleCalls);
        Assert.Equal(0, billingExecutor.HandleCalls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommandReceive_WhenRootInboxExecutorExistsButModuleRuntimeIsPartial_FailsAtStartup()
    {
        var fallbackExecutor = new CapturingInboxHandlerExecutor();
        var fulfillmentWriter = new CapturingModuleOutboxWriter("fulfillment");
        var services = new ServiceCollection();
        services.AddSingleton<IDurableInboxHandlerExecutor>(fallbackExecutor);
        RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
            fulfillmentWriter.ModuleName,
            _ => fulfillmentWriter));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.UseDurableMessaging();
                    module.UsePersistence("test persistence");
                    module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
                });
            }));

        Assert.Contains("missing durable module persistence role registrations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, fallbackExecutor.HandleCalls);
        Assert.Empty(fulfillmentWriter.Envelopes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CommandSender_WhenModuleOperationStoreIsMissing_ThrowsProviderSpecificError()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(new TestOutboxWriter());
        services.AddBondstone(bondstone =>
        {
            RegisterSalesSendingCommand(bondstone);
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        Guid durableOperationId = Guid.Parse("6916cddc-c0bc-47f3-9d33-0d92719d2f6b");

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "sales",
                    new TestTrackedSendingCommand(durableOperationId)));

        Assert.Contains(nameof(IDurableOperationStateStore), exception.Message, StringComparison.Ordinal);
        Assert.Contains("durable module operation-state store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("test persistence", exception.Message, StringComparison.Ordinal);
        Assert.Contains("module persistence services", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommandSender_WhenOnlyAnotherModuleOperationStoreExists_FailsAtStartup()
    {
        var salesWriter = new CapturingModuleOutboxWriter("sales");
        var fallbackStore = new CapturingOperationStateStore();
        var fulfillmentStore = new CapturingModuleOperationStateStore("fulfillment");
        var services = new ServiceCollection();
        RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
            salesWriter.ModuleName,
            _ => salesWriter));
        services.AddSingleton<IDurableOperationStateStore>(fallbackStore);
        RegisterOperationStateStore(services, new DurableModuleOperationStateStoreRegistration(
            fulfillmentStore.ModuleName,
            _ => fulfillmentStore));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                RegisterSalesSendingCommand(bondstone);
            }));

        Assert.Contains("missing durable module persistence role registrations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Empty(salesWriter.Envelopes);
        Assert.Empty(fallbackStore.SavedStates);
        Assert.Empty(fulfillmentStore.SavedStates);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommandSender_WhenRootOperationStoreExistsButModuleRuntimeIsPartial_FailsAtStartup()
    {
        var salesWriter = new CapturingModuleOutboxWriter("sales");
        var fallbackStore = new CapturingOperationStateStore();
        var services = new ServiceCollection();
        RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
            salesWriter.ModuleName,
            _ => salesWriter));
        services.AddSingleton<IDurableOperationStateStore>(fallbackStore);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                RegisterSalesSendingCommand(bondstone);
            }));

        Assert.Contains("missing durable module persistence role registrations", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Empty(salesWriter.Envelopes);
        Assert.Empty(fallbackStore.SavedStates);
    }

    private static void RegisterSalesSendingCommand(
        BondstoneBuilder bondstone)
    {
        bondstone.Module("sales", module =>
        {
            module.UseDurableMessaging();
            module.UsePersistence("test persistence");
            module.Commands.RegisterHandler<TestSendingCommand, TestSendingCommandHandler>();
            module.Commands.RegisterHandler<TestTrackedSendingCommand, TestTrackedSendingCommandHandler>();
        });
        bondstone.Module("fulfillment", module =>
        {
            module.UseDurableMessaging();
            module.UsePersistence("test persistence");
            module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
        });
    }

    private static void RegisterOutboxWriter(
        IServiceCollection services,
        DurableModuleOutboxWriterRegistration registration)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddOutboxWriter(registration);
    }

    private static void RegisterInboxHandlerExecutor(
        IServiceCollection services,
        DurableModuleInboxHandlerExecutorRegistration registration)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddInboxHandlerExecutor(registration);
    }

    private static void RegisterInboxInspectionStore(
        IServiceCollection services,
        DurableModuleInboxInspectionStoreRegistration registration)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddInboxInspectionStore(registration);
    }

    private static void RegisterOutboxDispatcher(
        IServiceCollection services,
        DurableModuleOutboxDispatcherRegistration registration)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddOutboxDispatcher(registration);
    }

    private static void RegisterOutboxInspectionStore(
        IServiceCollection services,
        DurableModuleOutboxInspectionStoreRegistration registration)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddOutboxInspectionStore(registration);
    }

    private static void RegisterOperationStateStore(
        IServiceCollection services,
        DurableModuleOperationStateStoreRegistration registration)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddOperationStateStore(registration);
    }

    private static DurableMessageEnvelope CreateTestCommandEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("49904d26-8a90-4e2f-8a9c-6cd649d9279e"),
            MessageKind.Command,
            "persistence.test.command.v1",
            "sales",
            "fulfillment",
            "{}",
            DateTimeOffset.Parse("2026-06-10T12:00:00+00:00"));
    }

    [DurableCommandIdentity("persistence.test.command.v1")]
    private sealed record TestCommand : IDurableCommand;

    private sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public ValueTask HandleAsync(
            TestCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("persistence.test.sending-command.v1")]
    private sealed record TestSendingCommand : IDurableCommand;

    private sealed class TestSendingCommandHandler(IDurableCommandSender sender)
        : ICommandHandler<TestSendingCommand>
    {
        public async ValueTask HandleAsync(
            TestSendingCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new TestCommand(),
                "fulfillment",
                ct);
        }
    }

    [DurableCommandIdentity("persistence.test.tracked-sending-command.v1")]
    private sealed record TestTrackedSendingCommand(Guid DurableOperationId) : IDurableCommand;

    private sealed class TestTrackedSendingCommandHandler(IDurableCommandSender sender)
        : ICommandHandler<TestTrackedSendingCommand>
    {
        public async ValueTask HandleAsync(
            TestTrackedSendingCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new TestCommand(),
                "fulfillment",
                partitionKey: null,
                durableOperationId: command.DurableOperationId,
                ct: ct);
        }
    }

    private sealed class TestOutboxWriter : IDurableOutboxWriter
    {
        public ValueTask WriteAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
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

    private sealed class CapturingModuleOutboxWriter(string moduleName)
        : IDurableOutboxWriter
    {
        public string ModuleName { get; } = moduleName;

        public List<DurableMessageEnvelope> Envelopes { get; } = [];

        public ValueTask WriteAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            Envelopes.Add(envelope);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestModuleOutboxWriter(string moduleName)
        : IDurableOutboxWriter
    {
        public string ModuleName { get; } = moduleName;

        public ValueTask WriteAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestModuleOutboxDispatcher
        : IDurableOutboxDispatcher
    {
        public ValueTask<DurableOutboxDispatchResult> DispatchAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestModuleOutboxInspectionStore
        : IDurableOutboxInspectionStore
    {
        public ValueTask<IReadOnlyList<DurableOutboxRecord>> FindTerminalFailedAsync(
            int maxCount = 100,
            DateTimeOffset? failedAtOrBeforeUtc = null,
            string? sourceModuleName = null,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingInboxHandlerExecutor : IDurableInboxHandlerExecutor
    {
        public int HandleCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            CancellationToken ct = default)
        {
            HandleCalls++;
            await handler(ct);
            return new DurableInboxHandleResult(
                DurableInboxHandleStatus.Handled,
                record.MarkProcessed(DateTimeOffset.UtcNow));
        }
    }

    private sealed class CapturingModuleInboxHandlerExecutor(string moduleName)
        : IDurableInboxHandlerExecutor
    {
        public string ModuleName { get; } = moduleName;

        public int HandleCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            CancellationToken ct = default)
        {
            HandleCalls++;
            await handler(ct);
            return new DurableInboxHandleResult(
                DurableInboxHandleStatus.Handled,
                record.MarkProcessed(DateTimeOffset.UtcNow));
        }
    }

    private sealed class TestModuleInboxHandlerExecutor(string moduleName)
        : IDurableInboxHandlerExecutor
    {
        public string ModuleName { get; } = moduleName;

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestModuleInboxInspectionStore
        : IDurableInboxInspectionStore
    {
        public ValueTask<IReadOnlyList<DurableInboxRecord>> FindUnprocessedAsync(
            int maxCount = 100,
            DateTimeOffset? receivedAtOrBeforeUtc = null,
            string? moduleName = null,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class CapturingOperationStateStore : IDurableOperationStateStore
    {
        public List<DurableOperationState> SavedStates { get; } = [];

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
            SavedStates.Add(state);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingModuleOperationStateStore(string moduleName)
        : IDurableOperationStateStore
    {
        public string ModuleName { get; } = moduleName;

        public List<DurableOperationState> SavedStates { get; } = [];

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
            SavedStates.Add(state);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestModuleOperationStateStore(string moduleName)
        : IDurableOperationStateStore
    {
        public string ModuleName { get; } = moduleName;

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }

        public ValueTask SaveAsync(
            DurableOperationState state,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }
}
