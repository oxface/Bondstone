using System.Diagnostics;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Tests;
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
    public async Task SendAsync_WhenCalledInsideModuleCommand_EmitsCommandSendActivity()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var activities = new List<Activity>();
        using ActivityListener listener = ActivityTestHelper.CreateActivityListener(
            "Bondstone.Modules",
            activities);
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);

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
        Activity activity = Assert.Single(
            activities,
            candidate => candidate.OperationName == "bondstone.command.send");
        Assert.Equal(ActivityKind.Producer, activity.Kind);
        Assert.Equal(envelope.MessageId.ToString("D"), ActivityTestHelper.GetTag(activity, "bondstone.message_id"));
        Assert.Equal("Command", ActivityTestHelper.GetTag(activity, "bondstone.message_kind"));
        Assert.Equal("sales.order.reserve.v1", ActivityTestHelper.GetTag(activity, "bondstone.message_type"));
        Assert.Equal("sales", ActivityTestHelper.GetTag(activity, "bondstone.source_module"));
        Assert.Equal("fulfillment", ActivityTestHelper.GetTag(activity, "bondstone.target_module"));
        Assert.Equal("order-123", ActivityTestHelper.GetTag(activity, "bondstone.partition_key"));
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
    public async Task SendAsync_WhenDurableOperationIdIsSupplied_ReturnsOperationHandle()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var operationStore = new CapturingOperationStateStore();
        var sendResultSink = new CapturingSendResultSink();
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);
        services.AddSingleton<IDurableOperationStateStore>(operationStore);
        services.AddSingleton(sendResultSink);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<
                    SubmitTrackedCapturedOrderCommand,
                    SubmitTrackedCapturedOrderHandler>();
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SubmitTrackedCapturedOrderCommand("order-123", durableOperationId));

        DurableCommandSendResult sendResult = Assert.Single(sendResultSink.Results);
        Assert.Equal(DurableCommandSendStatus.Accepted, sendResult.Status);
        Assert.Equal(durableOperationId, sendResult.DurableOperationId);
        Assert.NotNull(sendResult.Operation);
        Assert.Equal(durableOperationId, sendResult.Operation.DurableOperationId);
        Assert.Equal("sales", sendResult.Operation.SourceModule);
        Assert.Equal("fulfillment", sendResult.Operation.TargetModule);
        Assert.Equal("sales", sendResult.SourceModule);
        Assert.Equal("fulfillment", sendResult.TargetModule);
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
        RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
            salesWriter.ModuleName,
            _ => salesWriter));
        RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
            billingWriter.ModuleName,
            _ => billingWriter));
        RegisterInboxHandlerExecutor(services, "sales");
        RegisterInboxHandlerExecutor(services, "billing");
        RegisterOperationStateStore(services, new DurableModuleOperationStateStoreRegistration(
            "sales",
            _ => new NoOpOperationStateStore()));
        RegisterOperationStateStore(services, new DurableModuleOperationStateStoreRegistration(
            "billing",
            _ => new NoOpOperationStateStore()));
        RegisterOutboxDispatcher(services, "sales");
        RegisterOutboxDispatcher(services, "billing");

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
        RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
            salesWriter.ModuleName,
            _ => salesWriter));
        RegisterOutboxWriter(services, new DurableModuleOutboxWriterRegistration(
            "billing",
            _ => new CapturingModuleOutboxWriter("billing")));
        RegisterInboxHandlerExecutor(services, "sales");
        RegisterInboxHandlerExecutor(services, "billing");
        RegisterOperationStateStore(services, new DurableModuleOperationStateStoreRegistration(
            salesStore.ModuleName,
            _ => salesStore));
        RegisterOperationStateStore(services, new DurableModuleOperationStateStoreRegistration(
            billingStore.ModuleName,
            _ => billingStore));
        RegisterOutboxDispatcher(services, "sales");
        RegisterOutboxDispatcher(services, "billing");

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

    [DurableCommandIdentity("sales.order.submit-tracked-captured.v1")]
    public sealed record SubmitTrackedCapturedOrderCommand(
        string OrderId,
        Guid DurableOperationId) : IDurableCommand;

    public sealed class SubmitTrackedCapturedOrderHandler(
        IDurableCommandSender sender,
        CapturingSendResultSink sendResultSink)
        : ICommandHandler<SubmitTrackedCapturedOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitTrackedCapturedOrderCommand command,
            CancellationToken ct = default)
        {
            DurableCommandSendResult result = await sender.SendAsync(
                new ReserveOrderCommand(command.OrderId),
                "fulfillment",
                partitionKey: command.OrderId,
                durableOperationId: command.DurableOperationId,
                ct: ct);

            sendResultSink.Results.Add(result);
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

    public sealed class CapturingSendResultSink
    {
        public List<DurableCommandSendResult> Results { get; } = [];
    }

    private static void RegisterOutboxWriter(
        IServiceCollection services,
        DurableModuleOutboxWriterRegistration registration)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddOutboxWriter(registration);
    }

    private static void RegisterOperationStateStore(
        IServiceCollection services,
        DurableModuleOperationStateStoreRegistration registration)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddOperationStateStore(registration);
    }

    private static void RegisterInboxHandlerExecutor(
        IServiceCollection services,
        string moduleName)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddInboxHandlerExecutor(new DurableModuleInboxHandlerExecutorRegistration(
                moduleName,
                _ => new NoOpInboxHandlerExecutor()));
    }

    private static void RegisterOutboxDispatcher(
        IServiceCollection services,
        string moduleName)
    {
        services.GetOrAddDurableModulePersistenceRegistrationRegistry()
            .AddOutboxDispatcher(new DurableModuleOutboxDispatcherRegistration(
                moduleName,
                _ => new NoOpOutboxDispatcher()));
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
        : IDurableOperationStateStore
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

    private sealed class NoOpInboxHandlerExecutor : IDurableInboxHandlerExecutor
    {
        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            CancellationToken ct = default)
        {
            return new ValueTask<DurableInboxHandleResult>(
                new DurableInboxHandleResult(
                    DurableInboxHandleStatus.Handled,
                    record));
        }
    }

    private sealed class NoOpOutboxDispatcher : IDurableOutboxDispatcher
    {
        public ValueTask<DurableOutboxDispatchResult> DispatchAsync(
            string claimedBy,
            TimeSpan leaseDuration,
            int maxCount = 100,
            CancellationToken ct = default)
        {
            return new ValueTask<DurableOutboxDispatchResult>(
                new DurableOutboxDispatchResult(0, 0, 0, 0, 0));
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
