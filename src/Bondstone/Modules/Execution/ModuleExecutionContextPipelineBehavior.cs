using Bondstone.Messaging;

namespace Bondstone.Modules;

internal sealed class ModuleExecutionContextPipelineBehavior<TCommand>(
    ModuleExecutionContextAccessor executionContextAccessor)
    : IModuleCommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly ModuleExecutionContextAccessor _executionContextAccessor =
        executionContextAccessor ?? throw new ArgumentNullException(nameof(executionContextAccessor));

    public async ValueTask HandleAsync(
        TCommand command,
        ModuleCommandExecutionContext context,
        ModuleCommandPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        using IDisposable scope = _executionContextAccessor.Push(
            new ModuleExecutionContext(context.ModuleName));

        await next(ct);
    }
}
