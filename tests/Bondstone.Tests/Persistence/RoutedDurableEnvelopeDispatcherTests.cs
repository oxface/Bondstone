using Bondstone.Messaging;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class RoutedDurableEnvelopeDispatcherTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenOneRouteMatches_SendsThroughMatchingRoute()
    {
        var matchingRoute = new CapturingRoute("PrimaryRoute", canSend: true);
        var skippedRoute = new CapturingRoute("AlternateRoute", canSend: false);
        var dispatcher = new RoutedDurableEnvelopeDispatcher(
            [matchingRoute, skippedRoute]);
        DurableOutboxRecord record = CreateRecord();

        await dispatcher.DispatchAsync(record);

        Assert.Same(record, matchingRoute.Record);
        Assert.Null(skippedRoute.Record);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenMultipleRoutesMatch_ThrowsAmbiguousRoute()
    {
        var dispatcher = new RoutedDurableEnvelopeDispatcher(
            [
                new CapturingRoute("PrimaryRoute", canSend: true),
                new CapturingRoute("AlternateRoute", canSend: true),
            ]);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync(CreateRecord()));

        Assert.Contains("Multiple durable envelope dispatch routes", exception.Message, StringComparison.Ordinal);
        Assert.Contains("PrimaryRoute", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AlternateRoute", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DispatchAsync_WhenNoRoutesMatch_ThrowsActionableRouteError()
    {
        var dispatcher = new RoutedDurableEnvelopeDispatcher(
            [new CapturingRoute("PrimaryRoute", canSend: false)]);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await dispatcher.DispatchAsync(CreateRecord()));

        Assert.Contains("No durable envelope dispatch route", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment.order.reserve.v1", exception.Message, StringComparison.Ordinal);
        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Configure routing so exactly one adapter owns this durable message.", exception.Message, StringComparison.Ordinal);
    }

    private static DurableOutboxRecord CreateRecord()
    {
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("1cf43bb8-5a45-47fc-b933-aec642835143"),
            MessageKind.Command,
            "fulfillment.order.reserve.v1",
            "sales",
            "fulfillment",
            """{"orderId":"A-100"}""",
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"));

        return new DurableOutboxRecord(
            envelope,
            DateTimeOffset.Parse("2026-06-09T12:00:01+00:00"),
            new DurableOutboxDispatchState(
                DurableOutboxStatus.Processing,
                attemptCount: 1,
                claimedBy: "worker-1",
                claimedUntilUtc: DateTimeOffset.Parse("2026-06-09T12:05:00+00:00")));
    }

    private sealed class CapturingRoute(
        string transportName,
        bool canSend)
        : IDurableEnvelopeDispatchRoute
    {
        public string TransportName => transportName;

        public DurableOutboxRecord? Record { get; private set; }

        public bool CanSend(
            DurableOutboxRecord record)
        {
            return canSend;
        }

        public ValueTask DispatchAsync(
            DurableOutboxRecord record,
            CancellationToken ct = default)
        {
            Record = record;
            return ValueTask.CompletedTask;
        }
    }
}
