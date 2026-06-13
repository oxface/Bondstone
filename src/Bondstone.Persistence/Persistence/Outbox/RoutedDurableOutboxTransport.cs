using Bondstone.Utility;

namespace Bondstone.Persistence;

public sealed class RoutedDurableOutboxTransport(
    IEnumerable<IDurableOutboxTransportRoute> routes)
    : IDurableOutboxTransport
{
    private readonly IReadOnlyCollection<IDurableOutboxTransportRoute> _routes =
        routes?.ToArray() ?? throw new ArgumentNullException(nameof(routes));

    public async ValueTask SendAsync(
        DurableOutboxRecord record,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        IDurableOutboxTransportRoute[] matches = _routes
            .Where(route => route.CanSend(record))
            .ToArray();

        if (matches.Length == 1)
        {
            await matches[0].SendAsync(record, ct);
            return;
        }

        if (matches.Length == 0)
        {
            throw new InvalidOperationException(
                $"No durable outbox transport route can send {DescribeRecord(record)}.");
        }

        string transportNames = string.Join(
            "', '",
            matches.Select(static route => route.TransportName)
                .OrderBy(static transportName => transportName, StringComparer.Ordinal));

        throw new InvalidOperationException(
            $"Multiple durable outbox transport routes can send {DescribeRecord(record)}: '{transportNames}'. Configure routing so exactly one transport owns this durable message.");
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
