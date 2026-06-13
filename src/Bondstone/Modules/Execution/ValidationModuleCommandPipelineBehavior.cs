using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

internal sealed class ValidationModuleCommandPipelineBehavior<TCommand>(
    IServiceProvider serviceProvider,
    ModuleCommandValidatorRegistry validatorRegistry)
    : IModuleCommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly ModuleCommandValidatorRegistry _validatorRegistry =
        validatorRegistry ?? throw new ArgumentNullException(nameof(validatorRegistry));

    public async ValueTask HandleAsync(
        TCommand command,
        ModuleCommandExecutionContext context,
        ModuleCommandPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        foreach (ModuleCommandValidatorRegistration registration in _validatorRegistry.GetValidators(
            context.ModuleName,
            typeof(TCommand)))
        {
            ICommandValidator<TCommand> validator = registration.CreateValidator<TCommand>(
                _serviceProvider);
            await validator.ValidateAsync(command, ct);
        }

        await next(ct);
    }
}
