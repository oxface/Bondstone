using Bondstone.Messaging;

namespace Bondstone.Modules;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    ValueTask HandleAsync(
        TCommand command,
        CancellationToken ct = default);
}

public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    ValueTask<TResult> HandleAsync(
        TCommand command,
        CancellationToken ct = default);
}
