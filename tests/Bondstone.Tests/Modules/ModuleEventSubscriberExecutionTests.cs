using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Modules;

public sealed class ModuleEventSubscriberExecutionTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenSubscriberIsRegistered_InvokesHandlerInsideModuleContext()
    {
        var services = new ServiceCollection();
        services.AddSingleton<EventCallLog>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderProjectionHandler>(
                    "fulfillment.order-projection.v1");
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IModuleExecutionContextAccessor accessor =
            serviceProvider.GetRequiredService<IModuleExecutionContextAccessor>();

        Assert.Null(accessor.Current);

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleEventSubscriberExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    "sales.order.ready-for-projection.v1",
                    "fulfillment.order-projection.v1",
                    new OrderSubmittedEvent("order-123"));
        }

        EventCallLog log = serviceProvider.GetRequiredService<EventCallLog>();
        Assert.Equal(["handle:order-123:fulfillment"], log.Calls);
        Assert.Null(accessor.Current);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenApplicationPipelineBehaviorIsRegistered_RunsThroughSubscriberPipeline()
    {
        var services = new ServiceCollection();
        services.AddSingleton<EventCallLog>();
        services.AddScoped<
            IModuleEventSubscriberPipelineBehavior<OrderSubmittedEvent>,
            CapturingEventSubscriberBehavior>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderProjectionHandler>(
                    "fulfillment.order-projection.v1");
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<IModuleEventSubscriberExecutor>()
            .ExecuteAsync(
                "fulfillment",
                "sales.order.ready-for-projection.v1",
                "fulfillment.order-projection.v1",
                new OrderSubmittedEvent("order-123"));

        EventCallLog log = serviceProvider.GetRequiredService<EventCallLog>();
        Assert.Equal(
            [
                "behavior-before:fulfillment:fulfillment.order-projection.v1:fulfillment",
                "handle:order-123:fulfillment",
                "behavior-after:fulfillment:fulfillment.order-projection.v1:fulfillment",
            ],
            log.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenSystemAndApplicationBehaviorsAreRegistered_RunsSystemByOrderFirst()
    {
        var services = new ServiceCollection();
        services.AddSingleton<EventCallLog>();
        services.AddScoped<
            IModuleEventSubscriberSystemPipelineBehavior<OrderSubmittedEvent>,
            LateEventSubscriberSystemBehavior>();
        services.AddScoped<
            IModuleEventSubscriberSystemPipelineBehavior<OrderSubmittedEvent>,
            EarlyEventSubscriberSystemBehavior>();
        services.AddScoped<
            IModuleEventSubscriberPipelineBehavior<OrderSubmittedEvent>,
            OrderingEventSubscriberApplicationBehavior>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderingEventSubscriberHandler>(
                    "fulfillment.order-projection.v1");
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleEventSubscriberExecutor>()
            .ExecuteAsync(
                "fulfillment",
                "sales.order.ready-for-projection.v1",
                "fulfillment.order-projection.v1",
                new OrderSubmittedEvent("order-123"));

        Assert.Equal(
            [
                "system-early:before",
                "system-late:before",
                "application:before",
                "handle-ordering:order-123",
                "application:after",
                "system-late:after",
                "system-early:after",
            ],
            serviceProvider.GetRequiredService<EventCallLog>().Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenReceiveContextExists_UsesPerSubscriberInboxRecord()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor();
        var services = new ServiceCollection();
        services.AddSingleton<EventCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderProjectionHandler>(
                    "fulfillment.order-projection.v1");
            });
        });

        DurableInboxRecord inboxRecord = CreateEventInboxRecord();

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        ModuleEventSubscriberExecutionResult result = await scope.ServiceProvider
            .GetRequiredService<IModuleEventSubscriberExecutor>()
            .ExecuteAsync(
                "fulfillment",
                "sales.order.ready-for-projection.v1",
                "fulfillment.order-projection.v1",
                new OrderSubmittedEvent("order-123"),
                new ModuleEventSubscriberReceiveContext(inboxRecord));

        Assert.NotNull(result.ReceiveInboxResult);
        Assert.Equal(DurableInboxHandleStatus.Handled, result.ReceiveInboxResult.Status);
        Assert.Equal(inboxRecord.Key, inboxExecutor.Record?.Key);
        Assert.Equal("fulfillment", inboxExecutor.Record?.Key.ModuleName);
        Assert.Equal("fulfillment.order-projection.v1", inboxExecutor.Record?.Key.HandlerIdentity);

        EventCallLog log = serviceProvider.GetRequiredService<EventCallLog>();
        Assert.Equal(["handle:order-123:fulfillment"], log.Calls);
        Assert.Equal(1, inboxExecutor.HandlerCalls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenInboxRecordIsAlreadyProcessed_SkipsHandler()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor(
            DurableInboxHandleStatus.AlreadyProcessed);
        var services = new ServiceCollection();
        services.AddSingleton<EventCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderProjectionHandler>(
                    "fulfillment.order-projection.v1");
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        ModuleEventSubscriberExecutionResult result = await scope.ServiceProvider
            .GetRequiredService<IModuleEventSubscriberExecutor>()
            .ExecuteAsync(
                "fulfillment",
                "sales.order.ready-for-projection.v1",
                "fulfillment.order-projection.v1",
                new OrderSubmittedEvent("order-123"),
                new ModuleEventSubscriberReceiveContext(CreateEventInboxRecord()));

        Assert.Equal(DurableInboxHandleStatus.AlreadyProcessed, result.ReceiveInboxResult?.Status);
        Assert.Empty(serviceProvider.GetRequiredService<EventCallLog>().Calls);
        Assert.Equal(0, inboxExecutor.HandlerCalls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenInboxRecordTargetsDifferentModule_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<EventCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(
            new CapturingInboxHandlerExecutor());

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderProjectionHandler>(
                    "fulfillment.order-projection.v1");
            });
        });

        var inboxRecord = new DurableInboxRecord(
            DurableInboxMessageKey.ForEventSubscriber(
                Guid.Parse("a02f4505-52ba-4f31-a8f3-1d66da04d5f8"),
                "billing",
                "fulfillment.order-projection.v1"),
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"));

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleEventSubscriberExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    "sales.order.ready-for-projection.v1",
                    "fulfillment.order-projection.v1",
                    new OrderSubmittedEvent("order-123"),
                    new ModuleEventSubscriberReceiveContext(inboxRecord)));

        Assert.Contains("billing", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ExecuteAsync_WhenSubscriberIsMissing_Throws()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                ConfigureDurableMessaging(module);
                module.Events.RegisterSubscriber<OrderSubmittedEvent, OrderProjectionHandler>(
                    "fulfillment.order-projection.v1");
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleEventSubscriberExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    "sales.order.ready-for-projection.v1",
                    "fulfillment.missing.v1",
                    new OrderSubmittedEvent("order-123")));

        Assert.Contains("fulfillment.missing.v1", exception.Message, StringComparison.Ordinal);
    }

    private static DurableInboxRecord CreateEventInboxRecord()
    {
        return new DurableInboxRecord(
            DurableInboxMessageKey.ForEventSubscriber(
                Guid.Parse("a02f4505-52ba-4f31-a8f3-1d66da04d5f8"),
                "fulfillment",
                "fulfillment.order-projection.v1"),
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"));
    }

    private static void ConfigureDurableMessaging(BondstoneModuleBuilder module)
    {
        module.UseDurableMessaging();
        module.UsePersistence("test persistence");
    }

    public sealed class EventCallLog
    {
        public List<string> Calls { get; } = [];
    }

    [IntegrationEventIdentity("sales.order.ready-for-projection.v1")]
    public sealed record OrderSubmittedEvent(string OrderId) : IIntegrationEvent;

    public sealed class OrderProjectionHandler(
        EventCallLog log,
        IModuleExecutionContextAccessor executionContextAccessor)
        : IIntegrationEventHandler<OrderSubmittedEvent>
    {
        public ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            CancellationToken ct = default)
        {
            log.Calls.Add(
                $"handle:{integrationEvent.OrderId}:{executionContextAccessor.Current?.ModuleName}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class CapturingEventSubscriberBehavior(
        EventCallLog log,
        IModuleExecutionContextAccessor executionContextAccessor)
        : IModuleEventSubscriberPipelineBehavior<OrderSubmittedEvent>
    {
        public async ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            ModuleEventSubscriberExecutionContext context,
            ModuleEventSubscriberPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add(
                $"behavior-before:{context.ModuleName}:{context.SubscriberIdentity}:{executionContextAccessor.Current?.ModuleName}");
            await next(ct);
            log.Calls.Add(
                $"behavior-after:{context.ModuleName}:{context.SubscriberIdentity}:{executionContextAccessor.Current?.ModuleName}");
        }
    }

    public sealed class EarlyEventSubscriberSystemBehavior(EventCallLog log)
        : IModuleEventSubscriberSystemPipelineBehavior<OrderSubmittedEvent>
    {
        public int Order => 125;

        public async ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            ModuleEventSubscriberExecutionContext context,
            ModuleEventSubscriberPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add("system-early:before");
            await next(ct);
            log.Calls.Add("system-early:after");
        }
    }

    public sealed class LateEventSubscriberSystemBehavior(EventCallLog log)
        : IModuleEventSubscriberSystemPipelineBehavior<OrderSubmittedEvent>
    {
        public int Order => 150;

        public async ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            ModuleEventSubscriberExecutionContext context,
            ModuleEventSubscriberPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add("system-late:before");
            await next(ct);
            log.Calls.Add("system-late:after");
        }
    }

    public sealed class OrderingEventSubscriberApplicationBehavior(EventCallLog log)
        : IModuleEventSubscriberPipelineBehavior<OrderSubmittedEvent>
    {
        public async ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            ModuleEventSubscriberExecutionContext context,
            ModuleEventSubscriberPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add("application:before");
            await next(ct);
            log.Calls.Add("application:after");
        }
    }

    public sealed class OrderingEventSubscriberHandler(EventCallLog log)
        : IIntegrationEventHandler<OrderSubmittedEvent>
    {
        public ValueTask HandleAsync(
            OrderSubmittedEvent integrationEvent,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle-ordering:{integrationEvent.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CapturingInboxHandlerExecutor(
        DurableInboxHandleStatus status = DurableInboxHandleStatus.Handled)
        : IDurableInboxHandlerExecutor
    {
        public DurableInboxRecord? Record { get; private set; }

        public int HandlerCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            CancellationToken ct = default)
        {
            Record = record;

            if (status == DurableInboxHandleStatus.AlreadyProcessed)
            {
                return new DurableInboxHandleResult(
                    DurableInboxHandleStatus.AlreadyProcessed,
                    record.MarkProcessed(record.ReceivedAtUtc.AddMinutes(1)));
            }

            HandlerCalls++;
            await handler(ct);

            return new DurableInboxHandleResult(
                DurableInboxHandleStatus.Handled,
                record.MarkProcessed(record.ReceivedAtUtc.AddMinutes(1)));
        }
    }
}
