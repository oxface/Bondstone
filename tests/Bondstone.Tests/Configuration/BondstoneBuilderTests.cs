using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Configuration;

public sealed class BondstoneBuilderTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenServicesIsNull_Throws()
    {
        IServiceCollection? services = null;

        Assert.Throws<ArgumentNullException>(
            () => services!.AddBondstone(_ => { }));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenConfigureIsNull_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(
            () => services.AddBondstone(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDispatcherIsConfiguredWithoutPersistence_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(builder =>
            {
                builder.Outbox.MarkTransport("test transport");
                builder.Outbox.MarkDispatcher("test dispatcher");
            }));

        Assert.Contains("persistence provider", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDispatcherIsConfiguredWithoutTransport_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(builder =>
            {
                builder.Outbox.MarkPersistenceProvider("test persistence");
                builder.Outbox.MarkDispatcher("test dispatcher");
            }));

        Assert.Contains("outbox transport", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDispatcherHasPersistenceAndTransport_ReturnsServices()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddBondstone(builder =>
        {
            builder.Outbox.MarkPersistenceProvider("test persistence");
            builder.Outbox.MarkTransport("test transport");
            builder.Outbox.MarkDispatcher("test dispatcher");
        });

        Assert.Same(services, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenTransportOnlyIsConfigured_AllowsManualComposition()
    {
        var services = new ServiceCollection();

        IServiceCollection result = services.AddBondstone(
            builder => builder.Outbox.MarkTransport("test transport"));

        Assert.Same(services, result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDefaultMessageRegistryWasRegisteredByType_NormalizesToInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IMessageTypeRegistry, MessageTypeRegistry>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IMessageTypeRegistry registry = serviceProvider.GetRequiredService<IMessageTypeRegistry>();

        Assert.Equal(typeof(TestCommand), registry.ResolveClrType("sales.test.command.v1"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenModuleRegistryWasRegisteredByConsumer_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBondstoneModuleRegistry, ConsumerModuleRegistry>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(_ => { }));

        Assert.Contains(
            nameof(IBondstoneModuleRegistry),
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenModuleExecutionContextAccessorWasRegisteredByConsumer_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IModuleExecutionContextAccessor, ConsumerModuleExecutionContextAccessor>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(_ => { }));

        Assert.Contains(
            nameof(IModuleExecutionContextAccessor),
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MarkPersistenceProvider_WhenCapabilityNameIsBlank_Throws()
    {
        var services = new ServiceCollection();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => services.AddBondstone(builder =>
                builder.Outbox.MarkPersistenceProvider(" ")));

        Assert.Equal("providerName", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddConfigurationValidator_WhenRegistered_RunsAfterHostConfiguration()
    {
        var services = new ServiceCollection();
        var validator = new CapturingConfigurationValidator();

        services.AddBondstone(builder =>
        {
            builder.AddConfigurationValidator(validator);
            builder.Module("sales", _ => { });
        });

        Assert.Equal(["sales"], validator.ModuleNames);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddConfigurationValidator_WhenValidatorIsNull_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(
            () => services.AddBondstone(builder =>
                builder.AddConfigurationValidator(null!)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDurableMessagingModuleHasNoPersistence_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("sales", module =>
                {
                    module.UseDurableMessaging();
                });
            }));

        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("persistence", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenDurableHandlerModuleDoesNotUseDurableMessaging_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("sales", module =>
                {
                    module.Commands.RegisterHandler<TestCommand, TestCommandHandler>();
                });
            }));

        Assert.Contains("sales", exception.Message, StringComparison.Ordinal);
        Assert.Contains("durable messaging", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sales.test.command.v1", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddBondstone_WhenEventSubscriberModuleDoesNotUseDurableMessaging_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.Events.RegisterSubscriber<TestEvent, TestEventHandler>(
                        "fulfillment.test-event.v1");
                });
            }));

        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("durable messaging", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sales.test.event.v1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment.test-event.v1", exception.Message, StringComparison.Ordinal);
    }

    [DurableCommandIdentity("sales.test.command.v1")]
    public sealed record TestCommand : IDurableCommand;

    public sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public ValueTask HandleAsync(
            TestCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [IntegrationEventIdentity("sales.test.event.v1")]
    public sealed record TestEvent : IIntegrationEvent;

    public sealed class TestEventHandler : IIntegrationEventHandler<TestEvent>
    {
        public ValueTask HandleAsync(
            TestEvent integrationEvent,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ConsumerModuleRegistry : IBondstoneModuleRegistry
    {
        public IReadOnlyCollection<BondstoneModuleRegistration> Modules => [];

        public BondstoneModuleRegistration GetModule(string moduleName)
        {
            throw new NotSupportedException();
        }

        public bool TryGetModule(
            string moduleName,
            out BondstoneModuleRegistration? registration)
        {
            registration = null;
            return false;
        }
    }

    private sealed class ConsumerModuleExecutionContextAccessor : IModuleExecutionContextAccessor
    {
        public ModuleExecutionContext? Current => null;
    }

    private sealed class CapturingConfigurationValidator
        : IBondstoneConfigurationValidator
    {
        public IReadOnlyCollection<string> ModuleNames { get; private set; } = [];

        public void Validate(BondstoneConfigurationValidationContext context)
        {
            ModuleNames = context.Modules
                .Select(static module => module.Name)
                .OrderBy(static name => name, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
