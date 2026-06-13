namespace Bondstone.Messaging;

/// <summary>
/// Marker for a command accepted for durable asynchronous delivery through an outbox.
/// </summary>
public interface IDurableCommand : ICommand
{
}
