using Bondstone.Transport.Rebus.Outbox;
using Rebus.Extensions;
using Rebus.Handlers;
using Rebus.Pipeline;

namespace Bondstone.Transport.Rebus.Inbox;

public sealed class RebusModuleCommandEndpointHandler(
    RebusModuleCommandEndpointHandlerOptions options,
    IRebusDurableMessageEndpointDispatcher endpointDispatcher)
    : IHandleMessages<RebusDurableMessageEnvelope>
{
    private readonly RebusModuleCommandEndpointHandlerOptions _options =
        options ?? throw new ArgumentNullException(nameof(options));
    private readonly IRebusDurableMessageEndpointDispatcher _endpointDispatcher =
        endpointDispatcher ?? throw new ArgumentNullException(nameof(endpointDispatcher));

    public async Task Handle(RebusDurableMessageEnvelope message)
    {
        ArgumentNullException.ThrowIfNull(message);

        CancellationToken ct = MessageContext.Current?.GetCancellationToken() ?? default;
        await _endpointDispatcher.DispatchAsync(
            _options.EndpointName,
            message,
            ct);
    }
}
