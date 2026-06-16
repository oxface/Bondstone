using Bondstone.Configuration;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Hosting.Outbox;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Bondstone.Composition.Tests;

public sealed class AddBondstoneCompositionTests
{
    [Fact]
    [Trait("Category", "Application")]
    public void AddBondstone_WithPostgreSqlCustomDispatcherAndWorker_ComposesResolvableOutboxGraph()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<ILogger<DurableOutboxWorker>>(
            NullLogger<DurableOutboxWorker>.Instance);

        services.AddBondstone(bondstone =>
        {
            bondstone.UsePostgreSqlPersistence<CompositionDbContext>(
                "Host=localhost;Database=bondstone");
            bondstone.UseDurableEnvelopeDispatcher<RecordingEnvelopeDispatcher>();
            bondstone.Outbox.UseWorker(options =>
            {
                options.WorkerId = "composition-smoke-test";
                options.BatchSize = 10;
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        IHostedService hostedService = Assert.Single(
            serviceProvider.GetServices<IHostedService>());
        Assert.IsType<DurableOutboxWorker>(hostedService);

        using IServiceScope scope = serviceProvider.CreateScope();
        Assert.IsType<DurableOutboxDispatcher>(
            scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatcher>());
        Assert.IsType<RecordingEnvelopeDispatcher>(
            scope.ServiceProvider.GetRequiredService<IDurableEnvelopeDispatcher>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDurableOutboxClaimer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDurableOutboxLeaseRenewer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatchRecorder>());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task AddBondstone_WithModuleReceivePipeline_ComposesReceiveThroughInbox()
    {
        var inboxExecutor = new CapturingInboxHandlerExecutor();
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(inboxExecutor);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("composition test persistence");
                module.Commands.RegisterHandler<TypedCompositionCommand, TypedCompositionHandler>(
                    "fulfillment.order.reserve.v1");
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        using IServiceScope scope = serviceProvider.CreateScope();
        IModuleCommandReceivePipeline receivePipeline =
            scope.ServiceProvider.GetRequiredService<IModuleCommandReceivePipeline>();

        DurableInboxHandleResult result = await receivePipeline.HandleOnceAsync(CreateEnvelope());

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.NotNull(inboxExecutor.Record);
        Assert.Equal(CreateEnvelope().MessageId, inboxExecutor.Record.Key.MessageId);
        Assert.Equal("fulfillment", inboxExecutor.Record.Key.ModuleName);
        Assert.Equal("fulfillment.order.reserve.v1", inboxExecutor.Record.Key.HandlerIdentity);
        Assert.Equal(1, inboxExecutor.HandlerCalls);
        Assert.Equal(
            ["handle:A-100"],
            serviceProvider.GetRequiredService<CommandCallLog>().Calls);
    }

    private static DurableMessageEnvelope CreateEnvelope()
    {
        return new DurableMessageEnvelope(
            Guid.Parse("c370a6dd-9f1e-43b0-9506-a0f984ef03cf"),
            MessageKind.Command,
            "fulfillment.order.reserve.v1",
            "sales",
            "fulfillment",
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-05T12:00:00+00:00"),
            traceContext: new MessageTraceContext(
                "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00"),
            partitionKey: "orders/A-100");
    }

    private sealed class CompositionDbContext(DbContextOptions<CompositionDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstonePersistence();
        }
    }

    [DurableCommandIdentity("fulfillment.order.reserve.v1")]
    private sealed record TypedCompositionCommand(string OrderId) : IDurableCommand;

    [IntegrationEventIdentity("composition.event.v1")]
    private sealed record CompositionEvent(string Id) : IIntegrationEvent;

    private sealed class TypedCompositionHandler(CommandCallLog log)
        : ICommandHandler<TypedCompositionCommand>
    {
        public ValueTask HandleAsync(
            TypedCompositionCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle:{command.OrderId}");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class CommandCallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class CapturingInboxHandlerExecutor : IDurableInboxHandlerExecutor
    {
        public DurableInboxRecord? Record { get; private set; }

        public int HandlerCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            CancellationToken ct = default)
        {
            Record = record;
            HandlerCalls++;
            await handler(ct);

            return new DurableInboxHandleResult(
                DurableInboxHandleStatus.Handled,
                record.MarkProcessed(record.ReceivedAtUtc.AddMinutes(5)));
        }
    }

    private sealed class RecordingEnvelopeDispatcher : IDurableEnvelopeDispatcher
    {
        public DurableOutboxRecord? Record { get; private set; }

        public ValueTask DispatchAsync(
            DurableOutboxRecord record,
            CancellationToken ct = default)
        {
            Record = record;
            return ValueTask.CompletedTask;
        }
    }
}
