namespace Bondstone.Modules;

public interface IModuleEventSubscriberRegistry
{
    IReadOnlyCollection<ModuleEventSubscriberRegistration> Subscribers { get; }

    IReadOnlyCollection<ModuleEventSubscriberRegistration> GetByMessageTypeName(
        string messageTypeName);

    ModuleEventSubscriberRegistration GetSubscriber(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity);
}
