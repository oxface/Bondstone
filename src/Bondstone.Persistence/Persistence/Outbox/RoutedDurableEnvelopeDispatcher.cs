using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class RoutedDurableEnvelopeDispatcher(
    IEnumerable<IDurableEnvelopeDispatchRoute> routes)
    : IDurableEnvelopeDispatcher
{
    private readonly IReadOnlyCollection<IDurableEnvelopeDispatchRoute> _routes =
        routes?.ToArray() ?? throw new ArgumentNullException(nameof(routes));

    public async ValueTask DispatchAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        IDurableEnvelopeDispatchRoute[] matches = _routes
            .Where(route => route.CanSend(record))
            .ToArray();

        if (matches.Length == 1)
        {
            await matches[0].DispatchAsync(record, ct);
            return;
        }

        if (matches.Length == 0)
        {
            throw new InvalidOperationException(
                $"No durable envelope dispatch route can send {DescribeRecord(record)}. Configure routing so exactly one adapter owns this durable message.");
        }

        string routeNames = string.Join(
            "', '",
            matches.Select(static route => route.TransportName)
                .OrderBy(static routeName => routeName, StringComparer.Ordinal));

        throw new InvalidOperationException(
            $"Multiple durable envelope dispatch routes can send {DescribeRecord(record)}: '{routeNames}'. Configure routing so exactly one adapter owns this durable message.");
    }

    private static string DescribeRecord(
        DurableOutboxRecord record)
    {
        string messageTypeName = record.Envelope.MessageTypeName.NormalizeRequired(
            nameof(record.Envelope.MessageTypeName),
            "Message type name");

        return record.Envelope.TargetModule is null
            ? $"event '{messageTypeName}'"
            : $"command '{messageTypeName}' for target module '{record.Envelope.TargetModule}'";
    }
}
