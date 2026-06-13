namespace Bondstone.Modules;

public interface IModulePublishedEventRegistry
{
    IReadOnlyCollection<ModulePublishedEventRegistration> PublishedEvents { get; }
}
