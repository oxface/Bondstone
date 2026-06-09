namespace Bondstone.Messaging;

/// <summary>
/// Marker for an event published across module or service boundaries.
/// </summary>
public interface IIntegrationEvent : IMessage
{
}
