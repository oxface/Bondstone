using Bondstone.Modules;
using Bondstone.Utility;

namespace Bondstone.Configuration;

public sealed class BondstoneConfigurationValidationContext
{
    internal BondstoneConfigurationValidationContext(
        IReadOnlyCollection<BondstoneModuleRegistration> modules,
        IReadOnlyCollection<ModuleCommandRoute> commandRoutes,
        IReadOnlyCollection<ModulePublishedEventRegistration> publishedEvents,
        IReadOnlyCollection<ModuleEventSubscriberRegistration> eventSubscribers,
        int transportCount)
    {
        Modules = modules;
        CommandRoutes = commandRoutes;
        PublishedEvents = publishedEvents;
        EventSubscribers = eventSubscribers;
        TransportCount = transportCount;
        ModulesByName = modules.ToDictionary(
            static module => module.Name,
            StringComparer.Ordinal);
        DurableCommandRoutes = commandRoutes
            .Where(static route => route.IsDurable)
            .ToArray();
    }

    public IReadOnlyCollection<BondstoneModuleRegistration> Modules { get; }

    public IReadOnlyDictionary<string, BondstoneModuleRegistration> ModulesByName { get; }

    public IReadOnlyCollection<ModuleCommandRoute> CommandRoutes { get; }

    public IReadOnlyCollection<ModuleCommandRoute> DurableCommandRoutes { get; }

    public IReadOnlyCollection<ModulePublishedEventRegistration> PublishedEvents { get; }

    public IReadOnlyCollection<ModuleEventSubscriberRegistration> EventSubscribers { get; }

    public int TransportCount { get; }

    public bool HasSingleTransport => TransportCount == 1;

    public bool ModuleHasDurableCommandHandlers(string moduleName)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");

        return DurableCommandRoutes.Any(route =>
            route.ModuleName == normalizedModuleName);
    }

    public bool HasEventSubscriber(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity)
    {
        string normalizedModuleName = moduleName.NormalizeRequired(
            nameof(moduleName),
            "Module name");
        string normalizedMessageTypeName = messageTypeName.NormalizeRequired(
            nameof(messageTypeName),
            "Message type name");
        string normalizedSubscriberIdentity = subscriberIdentity.NormalizeRequired(
            nameof(subscriberIdentity),
            "Subscriber identity");

        return EventSubscribers.Any(subscriber =>
            subscriber.ModuleName == normalizedModuleName
            && subscriber.MessageTypeName == normalizedMessageTypeName
            && subscriber.SubscriberIdentity == normalizedSubscriberIdentity);
    }
}
