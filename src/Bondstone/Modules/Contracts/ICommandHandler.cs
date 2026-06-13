using Bondstone.Messaging;

namespace Bondstone.Modules;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    ValueTask HandleAsync(
        TCommand command,
        CancellationToken ct = default);
}
