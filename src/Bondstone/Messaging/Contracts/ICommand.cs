namespace Bondstone.Messaging;

/// <summary>
/// Marker for a command executed through a Bondstone module command pipeline.
/// </summary>
public interface ICommand : IMessage
{
}

/// <summary>
/// Marker for a command that produces a result when executed through a Bondstone module command pipeline.
/// </summary>
public interface ICommand<TResult> : ICommand
{
}
