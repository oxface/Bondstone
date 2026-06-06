using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

internal sealed class ValidationModuleCommandPipelineBehavior<TCommand>(
    IServiceProvider serviceProvider)
    : IModuleCommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public async ValueTask HandleAsync(
        TCommand command,
        ModuleCommandPipelineContext context,
        ModuleCommandPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        foreach (ICommandValidator<TCommand> validator in _serviceProvider.GetServices<ICommandValidator<TCommand>>())
        {
            await validator.ValidateAsync(command, ct);
        }

        await next(ct);
    }
}
