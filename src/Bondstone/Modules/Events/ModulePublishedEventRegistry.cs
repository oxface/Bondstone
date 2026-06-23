using Bondstone.Diagnostics;

namespace Bondstone.Modules;

internal sealed class ModulePublishedEventRegistry : IModulePublishedEventRegistry
{
    private readonly Dictionary<PublishedEventKey, ModulePublishedEventRegistration> _publishedEvents = [];

    public IReadOnlyCollection<ModulePublishedEventRegistration> PublishedEvents
    {
        get
        {
            lock (_publishedEvents)
            {
                return _publishedEvents.Values.ToArray();
            }
        }
    }

    internal ModulePublishedEventRegistration Register(
        ModulePublishedEventRegistration publishedEvent)
    {
        ArgumentNullException.ThrowIfNull(publishedEvent);

        var key = new PublishedEventKey(
            publishedEvent.ModuleName,
            publishedEvent.MessageTypeName);

        lock (_publishedEvents)
        {
            if (_publishedEvents.TryGetValue(key, out ModulePublishedEventRegistration? existing))
            {
                if (existing.EventType != publishedEvent.EventType)
                {
                    throw new BondstoneSetupException(
                        BondstoneSetupCodes.DuplicateDurableRegistration,
                        $"Module '{publishedEvent.ModuleName}' already has a published event registration for message type '{publishedEvent.MessageTypeName}'.");
                }

                return existing;
            }

            _publishedEvents.Add(key, publishedEvent);
            return publishedEvent;
        }
    }

    private sealed record PublishedEventKey(
        string ModuleName,
        string MessageTypeName);
}
