using Bondstone.Configuration;
using Bondstone.EntityFrameworkCore.Persistence;
using Bondstone.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Hosting.Outbox;
using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rebus.Bus.Advanced;
using Rebus.Routing;
using Xunit;

namespace Bondstone.Composition.Tests;

public sealed class AddBondstoneCompositionTests
{
    [Fact]
    [Trait("Category", "Application")]
    public void AddBondstone_WithPostgreSqlRebusAndWorker_ComposesResolvableOutboxGraph()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddSingleton<IRoutingApi, NoOpRoutingApi>();
        services.AddSingleton<ILogger<DurableOutboxWorker>>(
            NullLogger<DurableOutboxWorker>.Instance);

        services.AddBondstone(bondstone =>
        {
            bondstone.UsePostgreSqlPersistence<CompositionDbContext>(
                "Host=localhost;Database=bondstone");
            bondstone.UseRebusTransport(
                rebus => rebus.RouteModule("fulfillment").ToQueue("fulfillment-queue"));
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
        Assert.IsType<RebusDurableOutboxTransport>(
            scope.ServiceProvider.GetRequiredService<IDurableOutboxTransport>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDurableOutboxClaimer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDurableOutboxLeaseRenewer>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IDurableOutboxDispatchRecorder>());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task AddBondstone_WithPostgreSqlAndRebusInbox_ComposesReceiveWithExplicitCommitBoundary()
    {
        var coreExecutor = new CapturingInboxHandlerExecutor();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableInboxHandlerExecutor>(coreExecutor);

        services.AddBondstone(bondstone =>
        {
            bondstone.UsePostgreSqlPersistence<CompositionDbContext>(
                "Host=localhost;Database=bondstone");
            bondstone.UseRebusInbox();
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        using IServiceScope scope = serviceProvider.CreateScope();
        var receiveExecutor =
            scope.ServiceProvider.GetRequiredService<IRebusDurableInboxHandlerExecutor>();
        var persistenceScope =
            scope.ServiceProvider.GetRequiredService<IEntityFrameworkCorePersistenceScope>();
        DurableMessageEnvelope? handledEnvelope = null;

        DurableInboxHandleResult result = await receiveExecutor.HandleOnceAsync(
            CreateEnvelope(),
            "composition-handler",
            (message, _) =>
            {
                handledEnvelope = message;
                return ValueTask.CompletedTask;
            },
            persistenceScope.SaveChangesAsync);

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.NotNull(handledEnvelope);
        Assert.Equal(MessageKind.Command, handledEnvelope.MessageKind);
        Assert.NotNull(coreExecutor.Record);
        Assert.Equal(CreateEnvelope().MessageId, coreExecutor.Record.Key.MessageId);
        Assert.Equal("fulfillment", coreExecutor.Record.Key.ModuleName);
        Assert.Equal("composition-handler", coreExecutor.Record.Key.HandlerIdentity);
        Assert.Equal(1, coreExecutor.HandlerCalls);
        Assert.Equal(1, coreExecutor.CommitCalls);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task AddBondstone_WithPostgreSqlAndTypedRebusReceive_ComposesTypedReceivePipeline()
    {
        var rebusExecutor = new CapturingRebusInboxHandlerExecutor();
        var services = new ServiceCollection();
        services.AddSingleton<IRebusDurableInboxHandlerExecutor>(rebusExecutor);

        services.AddBondstone(bondstone =>
        {
            bondstone.UsePostgreSqlPersistence<CompositionDbContext>(
                "Host=localhost;Database=bondstone");
            bondstone.UseRebusTypedCommandReceivePipeline();
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        using IServiceScope scope = serviceProvider.CreateScope();
        scope.ServiceProvider
            .GetRequiredService<IMessageTypeRegistry>()
            .Register<TypedCompositionCommand>("fulfillment.order.reserve.v1");
        var receivePipeline =
            scope.ServiceProvider.GetRequiredService<IRebusTypedCommandReceivePipeline>();
        var persistenceScope =
            scope.ServiceProvider.GetRequiredService<IEntityFrameworkCorePersistenceScope>();
        TypedCompositionCommand? handledCommand = null;

        DurableInboxHandleResult result =
            await receivePipeline.HandleOnceAsync<TypedCompositionCommand>(
                CreateEnvelope(),
                "typed-composition-handler",
                (command, _) =>
                {
                    handledCommand = command;
                    return ValueTask.CompletedTask;
                },
                persistenceScope.SaveChangesAsync);

        Assert.Equal(DurableInboxHandleStatus.Handled, result.Status);
        Assert.Equal("A-100", handledCommand?.OrderId);
        Assert.Equal(CreateEnvelope().MessageId, rebusExecutor.Envelope?.MessageId);
        Assert.Equal("typed-composition-handler", rebusExecutor.HandlerIdentity);
        Assert.Equal(1, rebusExecutor.HandlerCalls);
        Assert.Equal(1, rebusExecutor.CommitCalls);
    }

    private static RebusDurableMessageEnvelope CreateEnvelope()
    {
        return new RebusDurableMessageEnvelope(
            Guid.Parse("c370a6dd-9f1e-43b0-9506-a0f984ef03cf"),
            MessageKind.Command.ToString(),
            "fulfillment.order.reserve.v1",
            "sales",
            "fulfillment",
            """{"orderId":"A-100"}""",
            null,
            DateTimeOffset.Parse("2026-06-05T12:00:00+00:00"),
            DurableOperationId: null,
            TraceParent: "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00",
            TraceState: null,
            TraceBaggage: null,
            CausationId: null,
            PartitionKey: "orders/A-100");
    }

    private sealed class CompositionDbContext(DbContextOptions<CompositionDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstonePersistence();
        }
    }

    private sealed record TypedCompositionCommand(string OrderId) : IDurableCommand;

    private sealed class NoOpRoutingApi : IRoutingApi
    {
        public Task Send(
            string destinationAddress,
            object explicitlyRoutedMessage,
            IDictionary<string, string> optionalHeaders = null!)
        {
            return Task.CompletedTask;
        }

        public Task SendRoutingSlip(
            Itinerary itinerary,
            object message,
            IDictionary<string, string> optionalHeaders = null!)
        {
            return Task.CompletedTask;
        }

        public Task Defer(
            string destinationAddress,
            TimeSpan delay,
            object message,
            IDictionary<string, string> optionalHeaders = null!)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class CapturingInboxHandlerExecutor : IDurableInboxHandlerExecutor
    {
        public DurableInboxRecord? Record { get; private set; }

        public int HandlerCalls { get; private set; }

        public int CommitCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            Func<CancellationToken, ValueTask> commit,
            CancellationToken ct = default)
        {
            Record = record;
            HandlerCalls++;
            await handler(ct);
            CommitCalls++;
            await commit(ct);

            return new DurableInboxHandleResult(
                DurableInboxHandleStatus.Handled,
                record.MarkProcessed(record.ReceivedAtUtc.AddMinutes(5)));
        }
    }

    private sealed class CapturingRebusInboxHandlerExecutor : IRebusDurableInboxHandlerExecutor
    {
        public RebusDurableMessageEnvelope? Envelope { get; private set; }

        public string? HandlerIdentity { get; private set; }

        public int HandlerCalls { get; private set; }

        public int CommitCalls { get; private set; }

        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            RebusDurableMessageEnvelope envelope,
            string handlerIdentity,
            Func<DurableMessageEnvelope, CancellationToken, ValueTask> handler,
            Func<CancellationToken, ValueTask> commit,
            CancellationToken ct = default)
        {
            Envelope = envelope;
            HandlerIdentity = handlerIdentity;
            HandlerCalls++;
            await handler(CreateDurableEnvelope(envelope), ct);
            CommitCalls++;
            await commit(ct);

            var record = new DurableInboxRecord(
                new DurableInboxMessageKey(
                    envelope.MessageId,
                    envelope.TargetModule!,
                    handlerIdentity),
                DateTimeOffset.Parse("2026-06-05T12:01:00+00:00"));

            return new DurableInboxHandleResult(
                DurableInboxHandleStatus.Handled,
                record.MarkProcessed(record.ReceivedAtUtc.AddMinutes(5)));
        }

        private static DurableMessageEnvelope CreateDurableEnvelope(
            RebusDurableMessageEnvelope envelope)
        {
            return new DurableMessageEnvelope(
                envelope.MessageId,
                MessageKind.Command,
                envelope.MessageTypeName,
                envelope.SourceModule,
                envelope.TargetModule,
                envelope.Payload,
                envelope.CreatedAtUtc,
                durableOperationId: envelope.DurableOperationId,
                traceContext: null,
                causationId: envelope.CausationId,
                partitionKey: envelope.PartitionKey,
                metadata: envelope.Metadata);
        }
    }
}
