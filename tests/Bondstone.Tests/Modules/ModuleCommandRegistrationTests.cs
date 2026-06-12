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
    public async Task ModuleCommands_WhenRuntimeAndApplicationBehaviorsAreRegistered_RunsRuntimeByOrderFirst()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddScoped<
            IModuleCommandPipelineBehavior<PipelineOrderCommand>,
            CommandApplicationBehavior>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.AddCommandPipelineContribution(
                    new ModuleCommandPipelineContribution(
                        "test.command.system-late",
                        ModulePipelineStepKind.System,
                        150,
                        typeof(LateCommandSystemBehavior)));
                module.AddCommandPipelineContribution(
                    new ModuleCommandPipelineContribution(
                        "test.command.system-early",
                        ModulePipelineStepKind.System,
                        125,
                        typeof(EarlyCommandSystemBehavior)));
                module.AddCommandPipelineContribution(
                    new ModuleCommandPipelineContribution(
                        "test.command.capability",
                        ModulePipelineStepKind.Capability,
                        140,
                        typeof(CommandCapabilityBehavior)));
                module.Commands.RegisterHandler<PipelineOrderCommand, PipelineOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new PipelineOrderCommand("order-123"));

        Assert.Equal(
            [
                "system-early:before",
                "capability:before",
                "system-late:before",
                "application:before:sales",
                "handle:order-123",
                "application:after:sales",
                "system-late:after",
                "capability:after",
                "system-early:after",
            ],
            serviceProvider.GetRequiredService<CommandCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ModuleCommands_WhenRuntimeContributionDoesNotApply_DoesNotCreateBehavior()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.AddCommandPipelineContribution(
                    new ModuleCommandPipelineContribution(
                        "test.command.non-selected",
                        ModulePipelineStepKind.System,
                        10,
                        null,
                        (_, _) => throw new InvalidOperationException(
                            "Should not create non-selected behavior.")));
            });

            bondstone.Module("sales", module =>
            {
                module.Commands.RegisterHandler<PipelineOrderCommand, PipelineOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new PipelineOrderCommand("order-123"));

        Assert.Equal(
            ["handle:order-123"],
            serviceProvider.GetRequiredService<CommandCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ModuleCommands_WhenRuntimeContributionsHaveSameOrder_ThrowsClearError()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("sales", module =>
                {
                    module.AddCommandPipelineContribution(
                        new ModuleCommandPipelineContribution(
                            "test.command.duplicate-a",
                            ModulePipelineStepKind.System,
                            777,
                            typeof(EarlyCommandSystemBehavior)));
                    module.AddCommandPipelineContribution(
                        new ModuleCommandPipelineContribution(
                            "test.command.duplicate-b",
                            ModulePipelineStepKind.Capability,
                            777,
                            typeof(LateCommandSystemBehavior)));
                    module.Commands.RegisterHandler<PipelineOrderCommand, PipelineOrderHandler>();
                });
            }));

        Assert.Contains("same order", exception.Message, StringComparison.Ordinal);
        Assert.Contains("test.command.duplicate-a", exception.Message, StringComparison.Ordinal);
        Assert.Contains("test.command.duplicate-b", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ModuleCommands_WhenRuntimeContributionsHaveSameName_ThrowsClearError()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("sales", module =>
                {
                    module.AddCommandPipelineContribution(
                        new ModuleCommandPipelineContribution(
                            "Bondstone.Command.ExecutionContext",
                            ModulePipelineStepKind.Capability,
                            999,
                            typeof(EarlyCommandSystemBehavior)));
                    module.Commands.RegisterHandler<PipelineOrderCommand, PipelineOrderHandler>();
                });
            }));

        Assert.Contains("same name", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Bondstone.Command.ExecutionContext", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ModuleCommandPipelineContribution_WhenBehaviorTypeIsInvalid_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new ModuleCommandPipelineContribution(
                "test.command.invalid",
                ModulePipelineStepKind.System,
                999,
                typeof(string)));

        Assert.Contains(
            nameof(IModuleCommandPipelineBehavior<PipelineOrderCommand>),
            exception.Message,
            StringComparison.Ordinal);
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

    public sealed record PipelineOrderCommand(string OrderId) : ICommand;

    public sealed class EarlyCommandSystemBehavior(CommandCallLog log)
        : IModuleCommandPipelineBehavior<PipelineOrderCommand>
    {
        public async ValueTask HandleAsync(
            PipelineOrderCommand command,
            ModuleCommandExecutionContext context,
            ModuleCommandPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add("system-early:before");
            await next(ct);
            log.Calls.Add("system-early:after");
        }
    }

    public sealed class LateCommandSystemBehavior(CommandCallLog log)
        : IModuleCommandPipelineBehavior<PipelineOrderCommand>
    {
        public async ValueTask HandleAsync(
            PipelineOrderCommand command,
            ModuleCommandExecutionContext context,
            ModuleCommandPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add("system-late:before");
            await next(ct);
            log.Calls.Add("system-late:after");
        }
    }

    public sealed class CommandCapabilityBehavior(CommandCallLog log)
        : IModuleCommandPipelineBehavior<PipelineOrderCommand>
    {
        public async ValueTask HandleAsync(
            PipelineOrderCommand command,
            ModuleCommandExecutionContext context,
            ModuleCommandPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add("capability:before");
            await next(ct);
            log.Calls.Add("capability:after");
        }
    }

    public sealed class CommandApplicationBehavior(
        CommandCallLog log,
        IModuleExecutionContextAccessor executionContextAccessor)
        : IModuleCommandPipelineBehavior<PipelineOrderCommand>
    {
        public async ValueTask HandleAsync(
            PipelineOrderCommand command,
            ModuleCommandExecutionContext context,
            ModuleCommandPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add($"application:before:{executionContextAccessor.Current?.ModuleName}");
            await next(ct);
            log.Calls.Add($"application:after:{executionContextAccessor.Current?.ModuleName}");
        }
    }

    public sealed class PipelineOrderHandler(CommandCallLog log)
        : ICommandHandler<PipelineOrderCommand>
    {
        public ValueTask HandleAsync(
            PipelineOrderCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle:{command.OrderId}");
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
