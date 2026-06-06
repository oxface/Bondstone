using Bondstone.Messaging;

namespace Bondstone.Modules;

public delegate ValueTask ModuleCommandPipelineNext(CancellationToken ct = default);

public interface IModuleCommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    ValueTask HandleAsync(
        TCommand command,
        ModuleCommandExecutionContext context,
        ModuleCommandPipelineNext next,
        CancellationToken ct = default);
}
