using Bondstone.Messaging;

namespace Bondstone.Modules;

/// <summary>
/// Handles a command that is executed through a Bondstone module command pipeline and does not return an application result.
/// </summary>
/// <typeparam name="TCommand">The command type handled by this handler.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="command">The command instance.</param>
    /// <param name="ct">A cancellation token for the handling operation.</param>
    ValueTask HandleAsync(
        TCommand command,
        CancellationToken ct = default);
}

/// <summary>
/// Handles a result-returning command that is executed through a Bondstone module command pipeline.
/// </summary>
/// <typeparam name="TCommand">The command type handled by this handler.</typeparam>
/// <typeparam name="TResult">The result type produced by the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>
    /// Handles the command and returns the typed application result.
    /// </summary>
    /// <param name="command">The command instance.</param>
    /// <param name="ct">A cancellation token for the handling operation.</param>
    /// <returns>The result produced by the command handler.</returns>
    ValueTask<TResult> HandleAsync(
        TCommand command,
        CancellationToken ct = default);
}
