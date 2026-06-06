using Bondstone.Messaging;

namespace Bondstone.Modules;

public interface IModuleCommandSystemPipelineBehavior<TCommand>
    : IModuleCommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    int Order { get; }
}

