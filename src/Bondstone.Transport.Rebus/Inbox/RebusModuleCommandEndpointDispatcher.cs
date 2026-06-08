using Bondstone.Persistence;
using Bondstone.Transport.Rebus.Outbox;
using Bondstone.Utility;

namespace Bondstone.Transport.Rebus.Inbox;

internal sealed class RebusModuleCommandEndpointDispatcher(
    IRebusModuleReceiveEndpointRegistry receiveEndpointRegistry,
    IRebusModuleCommandReceivePipeline receivePipeline)
    : IRebusModuleCommandEndpointDispatcher
{
    private readonly IRebusModuleReceiveEndpointRegistry _receiveEndpointRegistry =
        receiveEndpointRegistry ?? throw new ArgumentNullException(nameof(receiveEndpointRegistry));
    private readonly IRebusModuleCommandReceivePipeline _receivePipeline =
        receivePipeline ?? throw new ArgumentNullException(nameof(receivePipeline));

    public ValueTask<DurableInboxHandleResult> DispatchAsync(
        string endpointName,
        RebusDurableMessageEnvelope envelope,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(envelope);

        string normalizedEndpointName = endpointName.NormalizeRequired(
            nameof(endpointName),
            "Rebus receive endpoint name");
        string targetModule = envelope.TargetModule.NormalizeRequired(
            nameof(envelope.TargetModule),
            "Target module");

        RebusModuleReceiveEndpointBinding endpoint =
            _receiveEndpointRegistry.GetEndpoint(normalizedEndpointName);

        if (!_receiveEndpointRegistry.EndpointAcceptsModule(
            normalizedEndpointName,
            targetModule))
        {
            string acceptedModules = string.Join(
                "', '",
                endpoint.ModuleNames.OrderBy(static moduleName => moduleName, StringComparer.Ordinal));

            throw new InvalidOperationException(
                $"Rebus receive endpoint '{normalizedEndpointName}' does not accept target module '{targetModule}'. Accepted modules: '{acceptedModules}'.");
        }

        return _receivePipeline.HandleOnceAsync(envelope, ct);
    }
}
