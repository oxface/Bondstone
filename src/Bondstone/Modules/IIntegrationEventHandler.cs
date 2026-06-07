using Bondstone.Messaging;

namespace Bondstone.Modules;

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    ValueTask HandleAsync(
        TEvent integrationEvent,
        CancellationToken ct = default);
}
