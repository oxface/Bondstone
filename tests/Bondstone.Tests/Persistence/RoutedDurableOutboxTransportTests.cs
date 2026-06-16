using Bondstone.Messaging;
using Bondstone.Persistence;
using Xunit;

namespace Bondstone.Tests.Persistence;

public sealed class RoutedDurableOutboxTransportTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenOneRouteMatches_SendsThroughMatchingRoute()
    {
        var matchingRoute = new CapturingRoute("PrimaryRoute", canSend: true);
        var skippedRoute = new CapturingRoute("AlternateRoute", canSend: false);
        var transport = new RoutedDurableOutboxTransport(
            [matchingRoute, skippedRoute]);
        DurableOutboxRecord record = CreateRecord();

        await transport.SendAsync(record);

        Assert.Same(record, matchingRoute.Record);
        Assert.Null(skippedRoute.Record);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenMultipleRoutesMatch_ThrowsAmbiguousRoute()
    {
        var transport = new RoutedDurableOutboxTransport(
            [
                new CapturingRoute("PrimaryRoute", canSend: true),
                new CapturingRoute("AlternateRoute", canSend: true),
            ]);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await transport.SendAsync(CreateRecord()));

        Assert.Contains("Multiple durable outbox transport routes", exception.Message, StringComparison.Ordinal);
        Assert.Contains("PrimaryRoute", exception.Message, StringComparison.Ordinal);
        Assert.Contains("AlternateRoute", exception.Message, StringComparison.Ordinal);
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
        : IDurableOutboxTransportRoute
    {
        public string TransportName => transportName;

        public DurableOutboxRecord? Record { get; private set; }

        public bool CanSend(
            DurableOutboxRecord record)
        {
            return canSend;
        }

        public ValueTask SendAsync(
            DurableOutboxRecord record,
            CancellationToken ct = default)
        {
            Record = record;
            return ValueTask.CompletedTask;
        }
    }
}
