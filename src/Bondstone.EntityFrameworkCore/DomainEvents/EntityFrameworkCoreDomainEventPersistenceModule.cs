namespace Bondstone.EntityFrameworkCore.DomainEvents;

internal sealed record EntityFrameworkCoreDomainEventPersistenceModule(string ModuleName)
{
    public string ModuleName { get; } = string.IsNullOrWhiteSpace(ModuleName)
        ? throw new ArgumentException("Module name is required.", nameof(ModuleName))
        : ModuleName.Trim();
}
