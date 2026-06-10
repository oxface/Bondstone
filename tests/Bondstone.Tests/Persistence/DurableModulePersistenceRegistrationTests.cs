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
        services.AddSingleton<IDurableModuleOutboxWriter>(
            new TestModuleOutboxWriter("sales"));
        services.AddSingleton<IDurableModuleOutboxWriter>(
            new TestModuleOutboxWriter(" sales "));
        services.AddBondstone(_ => { });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IDurableCommandSender>());

        Assert.Contains("durable module outbox writer", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CommandPipelineBehavior_WhenDuplicateModuleInboxExecutorsAreRegistered_ThrowsClearError()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDurableModuleInboxHandlerExecutor>(
            new TestModuleInboxHandlerExecutor("fulfillment"));
        services.AddSingleton<IDurableModuleInboxHandlerExecutor>(
            new TestModuleInboxHandlerExecutor(" fulfillment "));
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => serviceProvider
                .GetServices<IModuleCommandSystemPipelineBehavior<TestCommand>>()
                .ToArray());

        Assert.Contains("durable module inbox handler executor", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OperationReader_WhenDuplicateModuleOperationStoresAreRegistered_ThrowsClearError()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDurableModuleOperationStateStore>(
            new TestModuleOperationStateStore("sales"));
        services.AddSingleton<IDurableModuleOperationStateStore>(
            new TestModuleOperationStateStore(" sales "));
        services.AddBondstone(_ => { });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => serviceProvider.GetRequiredService<IDurableOperationReader>());

        Assert.Contains("durable module operation-state store", exception.Message, StringComparison.Ordinal);
        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("exactly one", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void OutboxDispatchAggregator_WhenDuplicateModuleDispatchersAreRegistered_ThrowsClearError()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new DurableModuleOutboxDispatchAggregator(
            [
                new TestModuleOutboxDispatcher("sales"),
                new TestModuleOutboxDispatcher(" sales "),
            ]));

        Assert.Contains("durable module outbox dispatcher", exception.Message, StringComparison.Ordinal);
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

    private sealed class TestModuleOutboxWriter(string moduleName)
        : IDurableModuleOutboxWriter
    {
        public string ModuleName { get; } = moduleName;

        public ValueTask WriteAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestModuleOutboxDispatcher(string moduleName)
        : IDurableModuleOutboxDispatcher
    {
        public string ModuleName { get; } = moduleName;

        public ValueTask<DurableOutboxDispatchResult> DispatchAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestModuleInboxHandlerExecutor(string moduleName)
        : IDurableModuleInboxHandlerExecutor
    {
        public string ModuleName { get; } = moduleName;

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            Func<CancellationToken, ValueTask> commit,
            CancellationToken ct = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestModuleOperationStateStore(string moduleName)
        : IDurableModuleOperationStateStore
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
