using Bondstone.Messaging;
using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Inbox;
using Bondstone.Transport.Rebus.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Transport.Rebus.Tests.Inbox;

public sealed class RebusModuleCommandEndpointDispatcherTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenEndpointAcceptsTargetModule_DelegatesToReceivePipeline()
    {
        var result = CreateResult();
        var pipeline = new CapturingReceivePipeline(result);
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            pipeline,
            new RebusModuleReceiveEndpointBinding(
                "fulfillment-commands",
                ["fulfillment"]));
        IRebusModuleCommandEndpointDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRebusModuleCommandEndpointDispatcher>();

        using var cts = new CancellationTokenSource();
        RebusDurableMessageEnvelope envelope = CreateEnvelope();

        DurableInboxHandleResult actual = await dispatcher.DispatchAsync(
            "fulfillment-commands",
            envelope,
            cts.Token);

        Assert.Same(result, actual);
        Assert.Equal(1, pipeline.Calls);
        Assert.Same(envelope, pipeline.Envelope);
        Assert.Equal(cts.Token, pipeline.CancellationToken);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenEndpointIsNotConfigured_ThrowsBeforePipeline()
    {
        var pipeline = new CapturingReceivePipeline(CreateResult());
        await using ServiceProvider serviceProvider = CreateServiceProvider(pipeline);
        IRebusModuleCommandEndpointDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRebusModuleCommandEndpointDispatcher>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync(
                "missing-commands",
                CreateEnvelope()));

        Assert.Contains("missing-commands", exception.Message, StringComparison.Ordinal);
        Assert.Contains("not configured", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, pipeline.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenEndpointDoesNotAcceptTargetModule_ThrowsBeforePipeline()
    {
        var pipeline = new CapturingReceivePipeline(CreateResult());
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            pipeline,
            new RebusModuleReceiveEndpointBinding(
                "billing-commands",
                ["billing"]));
        IRebusModuleCommandEndpointDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRebusModuleCommandEndpointDispatcher>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync(
                "billing-commands",
                CreateEnvelope()));

        Assert.Contains("billing-commands", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("billing", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, pipeline.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenTargetModuleIsMissing_ThrowsBeforePipeline()
    {
        var pipeline = new CapturingReceivePipeline(CreateResult());
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            pipeline,
            new RebusModuleReceiveEndpointBinding(
                "fulfillment-commands",
                ["fulfillment"]));
        IRebusModuleCommandEndpointDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRebusModuleCommandEndpointDispatcher>();

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await dispatcher.DispatchAsync(
                "fulfillment-commands",
                CreateEnvelope(targetModule: null)));

        Assert.Contains("Target module", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, pipeline.Calls);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenCancellationIsRequested_ThrowsBeforePipeline()
    {
        var pipeline = new CapturingReceivePipeline(CreateResult());
        await using ServiceProvider serviceProvider = CreateServiceProvider(
            pipeline,
            new RebusModuleReceiveEndpointBinding(
                "fulfillment-commands",
                ["fulfillment"]));
        IRebusModuleCommandEndpointDispatcher dispatcher =
            serviceProvider.GetRequiredService<IRebusModuleCommandEndpointDispatcher>();

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await dispatcher.DispatchAsync(
                "fulfillment-commands",
                CreateEnvelope(),
                cts.Token));

        Assert.Equal(0, pipeline.Calls);
    }

    private static ServiceProvider CreateServiceProvider(
        CapturingReceivePipeline pipeline,
        params RebusModuleReceiveEndpointBinding[] endpoints)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IRebusModuleCommandReceivePipeline>(pipeline);
        services.AddBondstoneRebusModuleCommandReceiveTopology(endpoints);

        return services.BuildServiceProvider();
    }

    private static DurableInboxHandleResult CreateResult()
    {
        var record = new DurableInboxRecord(
            new DurableInboxMessageKey(
                Guid.Parse("e37baceb-4d6f-4c91-9870-6d209cd258a8"),
                "fulfillment",
                "fulfillment.order.reserve.v1"),
            DateTimeOffset.Parse("2026-06-08T12:00:00+00:00"));

        return new DurableInboxHandleResult(DurableInboxHandleStatus.Handled, record);
    }

    private static RebusDurableMessageEnvelope CreateEnvelope(
        string? targetModule = "fulfillment")
    {
        return new RebusDurableMessageEnvelope(
            Guid.Parse("e37baceb-4d6f-4c91-9870-6d209cd258a8"),
            MessageKind.Command.ToString(),
            "fulfillment.order.reserve.v1",
            "sales",
            targetModule,
            """{"orderId":"A-100"}""",
            null,
            DateTimeOffset.Parse("2026-06-08T12:00:00+00:00"),
            DurableOperationId: null,
            TraceParent: null,
            TraceState: null,
            TraceBaggage: null,
            CausationId: null,
            PartitionKey: "orders/A-100");
    }

    private sealed class CapturingReceivePipeline(DurableInboxHandleResult result)
        : IRebusModuleCommandReceivePipeline
    {
        public int Calls { get; private set; }

        public RebusDurableMessageEnvelope? Envelope { get; private set; }

        public CancellationToken CancellationToken { get; private set; }

        public ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            RebusDurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            Calls++;
            Envelope = envelope;
            CancellationToken = ct;
            return ValueTask.FromResult(result);
        }
    }
}
