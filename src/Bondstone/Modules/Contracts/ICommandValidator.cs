using Bondstone.Messaging;

namespace Bondstone.Modules;

public interface ICommandValidator<in TCommand>
    where TCommand : ICommand
{
    ValueTask ValidateAsync(
        TCommand command,
        CancellationToken ct = default);
}
