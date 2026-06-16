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
                ConfigureDurableMessaging(module);
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
                ConfigureDurableMessaging(module);
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
    public async Task ModuleCommands_WhenCommandReturnsResult_ReturnsTypedResultThroughPipeline()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Commands.RegisterHandler<
                    CreateDraftOrderResultCommand,
                    CreateDraftOrderResult,
                    CreateDraftOrderResultHandler>();
                module.Commands.RegisterValidator<
                    CreateDraftOrderResultCommand,
                    CreateDraftOrderResultValidator>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        ModuleCommandExecutionResult<CreateDraftOrderResult> result = await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteResultAsync(
                "sales",
                new CreateDraftOrderResultCommand("draft-123"));

        Assert.Equal(new CreateDraftOrderResult("draft-123", "created"), result.Result);
        Assert.Null(result.ReceiveInboxResult);
        Assert.Equal(
            ["validate-result-draft:draft-123", "handle-result-draft:draft-123"],
            serviceProvider.GetRequiredService<CommandCallLog>().Calls);

        IModuleCommandRouteRegistry routeRegistry =
            serviceProvider.GetRequiredService<IModuleCommandRouteRegistry>();
        ModuleCommandRoute route = routeRegistry.GetByCommandType(
            "sales",
            typeof(CreateDraftOrderResultCommand));

        Assert.False(route.IsDurable);
        Assert.Equal(typeof(CreateDraftOrderResult), route.ResultType);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleCommands_WhenSameCommandTypeHasValidatorsInDifferentModules_RunsOnlyExecutingModuleValidator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Commands.RegisterHandler<SharedCommand, SharedCommandHandler>();
                module.Commands.RegisterValidator<SharedCommand, SalesSharedCommandValidator>();
            });
            bondstone.Module("fulfillment", module =>
            {
                module.Commands.RegisterHandler<SharedCommand, SharedCommandHandler>();
                module.Commands.RegisterValidator<SharedCommand, FulfillmentSharedCommandValidator>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SharedCommand("shared-123"));

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(
            ["sales-validate:shared-123", "handle-shared:shared-123"],
            log.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleCommands_WhenExecuting_SetsModuleExecutionContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Commands.RegisterHandler<InspectContextCommand, InspectContextHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IModuleExecutionContextAccessor accessor =
            serviceProvider.GetRequiredService<IModuleExecutionContextAccessor>();

        Assert.Null(accessor.Current);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "sales",
                    new InspectContextCommand());
        }

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(["context:sales"], log.Calls);
        Assert.Null(accessor.Current);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleCommands_WhenHandlerExecutesSameModuleCommand_AllowsNestedLocalExecution()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Commands.RegisterHandler<
                    SameModuleOuterCommand,
                    SameModuleOuterHandler>();
                module.Commands.RegisterHandler<
                    SameModuleInnerCommand,
                    SameModuleInnerHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SameModuleOuterCommand("order-123"));

        Assert.Equal(
            ["outer-before:order-123", "inner:order-123", "outer-after:order-123"],
            serviceProvider.GetRequiredService<CommandCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleCommands_WhenHandlerExecutesDifferentModuleCommand_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Commands.RegisterHandler<
                    CrossModuleOuterCommand,
                    CrossModuleOuterHandler>();
            });
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "sales",
                    new CrossModuleOuterCommand("order-123")));

        Assert.Contains(
            "Local module command execution cannot cross module boundaries",
            exception.Message,
            StringComparison.Ordinal);
        Assert.Contains("Module 'sales'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("module 'fulfillment'", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Send a durable command", exception.Message, StringComparison.Ordinal);
        Assert.Equal(
            ["cross-before:order-123"],
            serviceProvider.GetRequiredService<CommandCallLog>().Calls);
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
                ConfigureDurableMessaging(module);
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

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RegisterFromAssemblyContaining_WhenResultHandlerExists_RegistersResultRoute()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                ConfigureDurableMessaging(module);
                module.Commands.RegisterFromAssemblyContaining<CreateDraftOrderResultCommand>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        ModuleCommandExecutionResult<CreateDraftOrderResult> result = await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteResultAsync(
                "sales",
                new CreateDraftOrderResultCommand("draft-456"));

        Assert.Equal(new CreateDraftOrderResult("draft-456", "created"), result.Result);
    }

    private sealed class FulfillmentModule : IBondstoneModule
    {
        public string Name => "fulfillment";

        public void Configure(BondstoneModuleBuilder module)
        {
            ConfigureDurableMessaging(module);
            module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
        }
    }

    private static void ConfigureDurableMessaging(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.UsePersistence("test persistence");
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

    public sealed record CreateDraftOrderResult(string DraftId, string Status);

    public sealed record CreateDraftOrderResultCommand(string DraftId)
        : ICommand<CreateDraftOrderResult>;

    public sealed class CreateDraftOrderResultHandler(CommandCallLog log)
        : ICommandHandler<CreateDraftOrderResultCommand, CreateDraftOrderResult>
    {
        public ValueTask<CreateDraftOrderResult> HandleAsync(
            CreateDraftOrderResultCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle-result-draft:{command.DraftId}");
            return ValueTask.FromResult(new CreateDraftOrderResult(
                command.DraftId,
                "created"));
        }
    }

    public sealed class CreateDraftOrderResultValidator(CommandCallLog log)
        : ICommandValidator<CreateDraftOrderResultCommand>
    {
        public ValueTask ValidateAsync(
            CreateDraftOrderResultCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"validate-result-draft:{command.DraftId}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed record SharedCommand(string Id) : ICommand;

    public sealed class SharedCommandHandler(CommandCallLog log)
        : ICommandHandler<SharedCommand>
    {
        public ValueTask HandleAsync(
            SharedCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle-shared:{command.Id}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class SalesSharedCommandValidator(CommandCallLog log)
        : ICommandValidator<SharedCommand>
    {
        public ValueTask ValidateAsync(
            SharedCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"sales-validate:{command.Id}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class FulfillmentSharedCommandValidator(CommandCallLog log)
        : ICommandValidator<SharedCommand>
    {
        public ValueTask ValidateAsync(
            SharedCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"fulfillment-validate:{command.Id}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed record InspectContextCommand : ICommand;

    public sealed class InspectContextHandler(
        IModuleExecutionContextAccessor executionContextAccessor,
        CommandCallLog log)
        : ICommandHandler<InspectContextCommand>
    {
        public ValueTask HandleAsync(
            InspectContextCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"context:{executionContextAccessor.Current?.ModuleName}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed record SameModuleOuterCommand(string OrderId) : ICommand;

    public sealed class SameModuleOuterHandler(
        IModuleCommandExecutor executor,
        CommandCallLog log)
        : ICommandHandler<SameModuleOuterCommand>
    {
        public async ValueTask HandleAsync(
            SameModuleOuterCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"outer-before:{command.OrderId}");
            await executor.ExecuteAsync(
                "sales",
                new SameModuleInnerCommand(command.OrderId),
                ct);
            log.Calls.Add($"outer-after:{command.OrderId}");
        }
    }

    public sealed record SameModuleInnerCommand(string OrderId) : ICommand;

    public sealed class SameModuleInnerHandler(CommandCallLog log)
        : ICommandHandler<SameModuleInnerCommand>
    {
        public ValueTask HandleAsync(
            SameModuleInnerCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"inner:{command.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed record CrossModuleOuterCommand(string OrderId) : ICommand;

    public sealed class CrossModuleOuterHandler(
        IModuleCommandExecutor executor,
        CommandCallLog log)
        : ICommandHandler<CrossModuleOuterCommand>
    {
        public async ValueTask HandleAsync(
            CrossModuleOuterCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"cross-before:{command.OrderId}");
            await executor.ExecuteAsync(
                "fulfillment",
                new ReserveOrderCommand(command.OrderId),
                ct);
            log.Calls.Add($"cross-after:{command.OrderId}");
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
