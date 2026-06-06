using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Modules;

public sealed class ModuleCommandRegistrationTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleCommands_WhenHandlerAndValidatorAreRegistered_ExecutesValidatorThenHandler()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
                module.Commands.RegisterValidator<ReserveOrderCommand, ReserveOrderValidator>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IModuleCommandExecutor executor = scope.ServiceProvider.GetRequiredService<IModuleCommandExecutor>();

        await executor.ExecuteAsync(
            " fulfillment ",
            new ReserveOrderCommand("order-123"));

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(["validate:order-123", "handle:order-123"], log.Calls);

        IModuleCommandRouteRegistry routeRegistry =
            serviceProvider.GetRequiredService<IModuleCommandRouteRegistry>();
        ModuleCommandRoute route = routeRegistry.GetByMessageTypeName(
            "fulfillment",
            "fulfillment.order.reserve.v1");

        Assert.Equal(typeof(ReserveOrderCommand), route.CommandType);
        Assert.Equal(typeof(ReserveOrderHandler), route.HandlerType);
        Assert.Equal("fulfillment.order.reserve.v1", route.HandlerIdentity);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterHandler_WithExplicitDurableMessageTypeName_RegistersMessageIdentityAndHandlerIdentity()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Commands.RegisterHandler<ShipOrderCommand, ShipOrderHandler>(
                    "sales.order.ship.v2",
                    handlerIdentity: "sales.ship-order-handler.v2");
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IModuleCommandRouteRegistry routeRegistry =
            serviceProvider.GetRequiredService<IModuleCommandRouteRegistry>();

        ModuleCommandRoute route = routeRegistry.GetByMessageTypeName(
            "sales",
            "sales.order.ship.v2");

        Assert.Equal(typeof(ShipOrderCommand), route.CommandType);
        Assert.Equal("sales.ship-order-handler.v2", route.HandlerIdentity);
        Assert.True(route.IsDurable);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterHandler_WhenCommandRouteAlreadyExists_Throws()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("sales", module =>
                {
                    module.Commands.RegisterHandler<CreateDraftOrderCommand, CreateDraftOrderHandler>();
                    module.Commands.RegisterHandler<CreateDraftOrderCommand, AlternateDraftOrderHandler>();
                });
            }));

        Assert.Contains("already has a command route", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleCommands_WhenCommandIsNotDurable_ExecutesThroughPipelineWithoutMessageIdentity()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Commands.RegisterHandler<CreateDraftOrderCommand, CreateDraftOrderHandler>();
                module.Commands.RegisterValidator<CreateDraftOrderCommand, CreateDraftOrderValidator>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new CreateDraftOrderCommand("draft-123"));

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(["validate-draft:draft-123", "handle-draft:draft-123"], log.Calls);

        IModuleCommandRouteRegistry routeRegistry =
            serviceProvider.GetRequiredService<IModuleCommandRouteRegistry>();
        ModuleCommandRoute route = routeRegistry.GetByCommandType(
            "sales",
            typeof(CreateDraftOrderCommand));

        Assert.False(route.IsDurable);
        Assert.Null(route.MessageTypeName);
        Assert.Null(route.HandlerIdentity);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RegisterFromAssemblyContaining_WhenHandlersAndValidatorsExist_RegistersRoutesAndValidators()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("billing", module =>
            {
                module.Commands.RegisterFromAssemblyContaining<CapturePaymentCommand>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "billing",
                new CapturePaymentCommand("payment-123"));

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(["validate-payment:payment-123", "handle-payment:payment-123"], log.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void AddModule_WhenModuleProvidesCommands_RegistersThemWithHostBuilder()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.AddModule(new FulfillmentModule());
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IModuleCommandRouteRegistry routeRegistry =
            serviceProvider.GetRequiredService<IModuleCommandRouteRegistry>();

        ModuleCommandRoute route = routeRegistry.GetByCommandType(
            "fulfillment",
            typeof(ReserveOrderCommand));

        Assert.Equal("fulfillment", route.ModuleName);
    }

    private sealed class FulfillmentModule : IBondstoneModule
    {
        public string Name => "fulfillment";

        public void Configure(BondstoneModuleBuilder module)
        {
            module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
        }
    }

    public sealed class CommandCallLog
    {
        public List<string> Calls { get; } = [];
    }

    [DurableCommandIdentity("fulfillment.order.reserve.v1")]
    public sealed record ReserveOrderCommand(string OrderId) : IDurableCommand;

    public sealed class ReserveOrderHandler(CommandCallLog log)
        : ICommandHandler<ReserveOrderCommand>
    {
        public ValueTask HandleAsync(
            ReserveOrderCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle:{command.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ReserveOrderValidator(CommandCallLog log)
        : ICommandValidator<ReserveOrderCommand>
    {
        public ValueTask ValidateAsync(
            ReserveOrderCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"validate:{command.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("billing.payment.capture.v1")]
    public sealed record CapturePaymentCommand(string PaymentId) : IDurableCommand;

    public sealed class CapturePaymentHandler(CommandCallLog log)
        : ICommandHandler<CapturePaymentCommand>
    {
        public ValueTask HandleAsync(
            CapturePaymentCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle-payment:{command.PaymentId}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class CapturePaymentValidator(CommandCallLog log)
        : ICommandValidator<CapturePaymentCommand>
    {
        public ValueTask ValidateAsync(
            CapturePaymentCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"validate-payment:{command.PaymentId}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed record CreateDraftOrderCommand(string DraftId) : ICommand;

    public sealed class CreateDraftOrderHandler(CommandCallLog log)
        : ICommandHandler<CreateDraftOrderCommand>
    {
        public ValueTask HandleAsync(
            CreateDraftOrderCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle-draft:{command.DraftId}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class CreateDraftOrderValidator(CommandCallLog log)
        : ICommandValidator<CreateDraftOrderCommand>
    {
        public ValueTask ValidateAsync(
            CreateDraftOrderCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"validate-draft:{command.DraftId}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class AlternateDraftOrderHandler : ICommandHandler<CreateDraftOrderCommand>
    {
        public ValueTask HandleAsync(
            CreateDraftOrderCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("sales.order.ship.v2")]
    public sealed record ShipOrderCommand(string OrderId) : IDurableCommand;

    public sealed class ShipOrderHandler : ICommandHandler<ShipOrderCommand>
    {
        public ValueTask HandleAsync(
            ShipOrderCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }
}
