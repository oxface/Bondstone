namespace Bondstone.DomainEvents;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class DomainEventIdentityAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
