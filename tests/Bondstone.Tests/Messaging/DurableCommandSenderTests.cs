using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableCommandSenderTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenCalledInsideModuleCommand_StagesEnvelopeWithCurrentModuleAsSource()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);
        services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-06T12:00:00+00:00")));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<SubmitOrderCommand, SubmitOrderHandler>();
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SubmitOrderCommand("order-123"));

        DurableMessageEnvelope envelope = Assert.Single(outboxWriter.Envelopes);
        Assert.Equal(MessageKind.Command, envelope.MessageKind);
        Assert.Equal("sales.order.reserve.v1", envelope.MessageTypeName);
        Assert.Equal("sales", envelope.SourceModule);
        Assert.Equal("fulfillment", envelope.TargetModule);
        Assert.Equal("""{"orderId":"order-123"}""", envelope.Payload);
        Assert.Equal("order-123", envelope.PartitionKey);
        Assert.Equal(DateTimeOffset.Parse("2026-06-06T12:00:00+00:00"), envelope.CreatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_UsesConfiguredDurablePayloadJsonOptions()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);
        services.ConfigureBondstoneDurablePayloadJson(
            options => options.Converters.Add(new DurableOrderIdJsonConverter()));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<
                    SubmitConvertedOrderCommand,
                    SubmitConvertedOrderHandler>();
                module.Commands.RegisterHandler<
                    ReserveConvertedOrderCommand,
                    ReserveConvertedOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SubmitConvertedOrderCommand("order-123"));

        DurableMessageEnvelope envelope = Assert.Single(outboxWriter.Envelopes);
        Assert.Equal("sales.order.reserve-converted.v1", envelope.MessageTypeName);
        Assert.Equal("""{"orderId":"payload-order-123"}""", envelope.Payload);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenDurableOperationIdIsSupplied_StagesPendingOperationState()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var operationStore = new CapturingOperationStateStore();
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);
        services.AddSingleton<IDurableOperationStateStore>(operationStore);
        services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-08T12:00:00+00:00")));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<
                    SubmitTrackedOrderCommand,
                    SubmitTrackedOrderHandler>();
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SubmitTrackedOrderCommand("order-123", durableOperationId));

        DurableMessageEnvelope envelope = Assert.Single(outboxWriter.Envelopes);
        Assert.Equal(durableOperationId, envelope.DurableOperationId);
        DurableOperationState state = Assert.Single(operationStore.SavedStates);
        Assert.Equal(durableOperationId, state.DurableOperationId);
        Assert.Equal(DurableOperationStatus.Pending, state.Status);
        Assert.Equal(DateTimeOffset.Parse("2026-06-08T12:00:00+00:00"), state.UpdatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenDurableOperationAlreadyExists_DoesNotDowngradeState()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var operationStore = new CapturingOperationStateStore();
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        operationStore.State = new DurableOperationState(
            durableOperationId,
            DurableOperationStatus.Completed,
            DateTimeOffset.Parse("2026-06-08T12:01:00+00:00"));
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);
        services.AddSingleton<IDurableOperationStateStore>(operationStore);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<
                    SubmitTrackedOrderCommand,
                    SubmitTrackedOrderHandler>();
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SubmitTrackedOrderCommand("order-123", durableOperationId));

        Assert.Single(outboxWriter.Envelopes);
        Assert.Empty(operationStore.SavedStates);
        Assert.Equal(DurableOperationStatus.Completed, operationStore.State.Status);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenNoModuleExecutionContextExists_Throws()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableCommandSender sender =
            scope.ServiceProvider.GetRequiredService<IDurableCommandSender>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sender.SendAsync(
                new ReserveOrderCommand("order-123"),
                "fulfillment"));

        Assert.Contains("module execution context", exception.Message, StringComparison.Ordinal);
        Assert.Empty(outboxWriter.Envelopes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenDurableOperationIdIsSuppliedWithoutStore_Throws()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<
                    SubmitTrackedOrderCommand,
                    SubmitTrackedOrderHandler>();
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
                    new SubmitTrackedOrderCommand(
                        "order-123",
                        Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766"))));

        Assert.Contains(nameof(IDurableOperationStateStore), exception.Message, StringComparison.Ordinal);
        Assert.Empty(outboxWriter.Envelopes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenModuleWritersAreRegistered_UsesCurrentModuleOutboxWriter()
    {
        var salesWriter = new CapturingModuleOutboxWriter("sales");
        var billingWriter = new CapturingModuleOutboxWriter("billing");
        var services = new ServiceCollection();
        services.AddSingleton<IDurableModuleOutboxWriter>(salesWriter);
        services.AddSingleton<IDurableModuleOutboxWriter>(billingWriter);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<SubmitOrderCommand, SubmitOrderHandler>();
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });

            bondstone.Module("billing", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<BillOrderCommand, BillOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SubmitOrderCommand("order-123"));

        DurableMessageEnvelope envelope = Assert.Single(salesWriter.Envelopes);
        Assert.Equal("sales", envelope.SourceModule);
        Assert.Empty(billingWriter.Envelopes);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenModuleOperationStoresAreRegistered_UsesCurrentModuleStore()
    {
        var salesWriter = new CapturingModuleOutboxWriter("sales");
        var salesStore = new CapturingModuleOperationStateStore("sales");
        var billingStore = new CapturingModuleOperationStateStore("billing");
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var services = new ServiceCollection();
        services.AddSingleton<IDurableModuleOutboxWriter>(salesWriter);
        services.AddSingleton<IDurableModuleOperationStateStore>(salesStore);
        services.AddSingleton<IDurableModuleOperationStateStore>(billingStore);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<
                    SubmitTrackedOrderCommand,
                    SubmitTrackedOrderHandler>();
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });

            bondstone.Module("billing", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<BillOrderCommand, BillOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SubmitTrackedOrderCommand("order-123", durableOperationId));

        Assert.Single(salesWriter.Envelopes);
        DurableOperationState state = Assert.Single(salesStore.SavedStates);
        Assert.Equal(durableOperationId, state.DurableOperationId);
        Assert.Equal(DurableOperationStatus.Pending, state.Status);
        Assert.Empty(billingStore.SavedStates);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task OperationReader_WhenModuleStoresHavePendingAndCompleted_ReturnsCompletedState()
    {
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var salesStore = new CapturingModuleOperationStateStore("sales")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Pending,
                DateTimeOffset.Parse("2026-06-08T12:00:00+00:00")),
        };
        var fulfillmentStore = new CapturingModuleOperationStateStore("fulfillment")
        {
            State = new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                DateTimeOffset.Parse("2026-06-08T12:01:00+00:00")),
        };
        var services = new ServiceCollection();
        services.AddSingleton<IDurableModuleOperationStateStore>(salesStore);
        services.AddSingleton<IDurableModuleOperationStateStore>(fulfillmentStore);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        DurableOperationState? state = await scope.ServiceProvider
            .GetRequiredService<IDurableOperationReader>()
            .GetStateAsync(durableOperationId);

        Assert.NotNull(state);
        Assert.Equal(DurableOperationStatus.Completed, state.Status);
    }

    [DurableCommandIdentity("sales.order.submit.v1")]
    public sealed record SubmitOrderCommand(string OrderId) : IDurableCommand;

    public sealed class SubmitOrderHandler(IDurableCommandSender sender)
        : ICommandHandler<SubmitOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitOrderCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new ReserveOrderCommand(command.OrderId),
                "fulfillment",
                partitionKey: command.OrderId,
                ct: ct);
        }
    }

    [DurableCommandIdentity("sales.order.submit-tracked.v1")]
    public sealed record SubmitTrackedOrderCommand(
        string OrderId,
        Guid DurableOperationId) : IDurableCommand;

    public sealed class SubmitTrackedOrderHandler(IDurableCommandSender sender)
        : ICommandHandler<SubmitTrackedOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitTrackedOrderCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new ReserveOrderCommand(command.OrderId),
                "fulfillment",
                partitionKey: command.OrderId,
                durableOperationId: command.DurableOperationId,
                ct: ct);
        }
    }

    [DurableCommandIdentity("sales.order.reserve.v1")]
    public sealed record ReserveOrderCommand(string OrderId) : IDurableCommand;

    public sealed class ReserveOrderHandler : ICommandHandler<ReserveOrderCommand>
    {
        public ValueTask HandleAsync(
            ReserveOrderCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("billing.order.bill.v1")]
    public sealed record BillOrderCommand(string OrderId) : IDurableCommand;

    public sealed class BillOrderHandler : ICommandHandler<BillOrderCommand>
    {
        public ValueTask HandleAsync(
            BillOrderCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("sales.order.submit-converted.v1")]
    public sealed record SubmitConvertedOrderCommand(string OrderId) : IDurableCommand;

    public sealed class SubmitConvertedOrderHandler(IDurableCommandSender sender)
        : ICommandHandler<SubmitConvertedOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitConvertedOrderCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new ReserveConvertedOrderCommand(new DurableOrderId(command.OrderId)),
                "fulfillment",
                ct);
        }
    }

    [DurableCommandIdentity("sales.order.reserve-converted.v1")]
    public sealed record ReserveConvertedOrderCommand(DurableOrderId OrderId)
        : IDurableCommand;

    public sealed class ReserveConvertedOrderHandler
        : ICommandHandler<ReserveConvertedOrderCommand>
    {
        public ValueTask HandleAsync(
            ReserveConvertedOrderCommand command,
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
        : IDurableModuleOutboxWriter
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

    private sealed class CapturingOperationStateStore : IDurableOperationStateStore
    {
        public DurableOperationState? State { get; set; }

        public List<DurableOperationState> SavedStates { get; } = [];

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult(
                State?.DurableOperationId == durableOperationId
                    ? State
                    : null);
        }

        public ValueTask SaveAsync(
            DurableOperationState state,
            CancellationToken ct = default)
        {
            State = state;
            SavedStates.Add(state);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingModuleOperationStateStore(string moduleName)
        : IDurableModuleOperationStateStore
    {
        public string ModuleName { get; } = moduleName;

        public DurableOperationState? State { get; set; }

        public List<DurableOperationState> SavedStates { get; } = [];

        public ValueTask<DurableOperationState?> GetStateAsync(
            Guid durableOperationId,
            CancellationToken ct = default)
        {
            return ValueTask.FromResult(
                State?.DurableOperationId == durableOperationId
                    ? State
                    : null);
        }

        public ValueTask SaveAsync(
            DurableOperationState state,
            CancellationToken ct = default)
        {
            State = state;
            SavedStates.Add(state);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
