using Bondstone.Messaging;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Outbox;

public sealed class RebusCommandDestinationDiagnostic
{
    public RebusCommandDestinationDiagnostic(
        string targetModule,
        RebusCommandDestinationSource source,
        string? destinationAddress = null,
        string? receiveEndpointName = null,
        string? failureReason = null)
    {
        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Rebus command destination source is not supported.");
        }

        TargetModule = targetModule.NormalizeRequired(
            nameof(targetModule),
            "Target module");
        Source = source;
        DestinationAddress = destinationAddress?.NormalizeRequired(
            nameof(destinationAddress),
            "Rebus destination address");
        ReceiveEndpointName = receiveEndpointName?.NormalizeRequired(
            nameof(receiveEndpointName),
            "Rebus receive endpoint name");
        FailureReason = failureReason;
    }

    public DurableMessageTopologyDiagnosticKind Kind =>
        DurableMessageTopologyDiagnosticKind.CommandDestination;

    public string TargetModule { get; }

    public RebusCommandDestinationSource Source { get; }

    public string? DestinationAddress { get; }

    public string? ReceiveEndpointName { get; }

    public string? FailureReason { get; }

    public bool HasDestination => DestinationAddress is not null;
}
