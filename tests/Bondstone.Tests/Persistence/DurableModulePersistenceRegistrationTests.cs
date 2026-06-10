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
