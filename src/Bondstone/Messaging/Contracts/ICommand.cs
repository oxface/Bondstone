namespace Bondstone.Messaging;

/// <summary>
/// Marker for a command executed through a Bondstone module command pipeline.
/// </summary>
public interface ICommand : IMessage
{
}

/// <summary>
/// Marker for a command that produces a typed result when executed through a Bondstone module command pipeline.
/// </summary>
/// <typeparam name="TResult">The result type returned by the command handler during in-process module execution.</typeparam>
public interface ICommand<TResult> : ICommand
{
}
