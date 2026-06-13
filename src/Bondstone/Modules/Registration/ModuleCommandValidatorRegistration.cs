using Bondstone.Messaging;
using Bondstone.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

internal sealed class ModuleCommandValidatorRegistration
{
    public ModuleCommandValidatorRegistration(
        string moduleName,
        Type commandType,
        Type validatorType)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentNullException.ThrowIfNull(validatorType);

        if (!typeof(ICommand).IsAssignableFrom(commandType))
        {
            throw new ArgumentException(
                $"Command type '{commandType.FullName}' must implement '{typeof(ICommand).FullName}'.",
                nameof(commandType));
        }

        Type validatorInterface = typeof(ICommandValidator<>).MakeGenericType(commandType);
        if (!validatorInterface.IsAssignableFrom(validatorType))
        {
            throw new ArgumentException(
                $"Validator type '{validatorType.FullName}' must implement '{validatorInterface.FullName}'.",
                nameof(validatorType));
        }

        ModuleName = moduleName.NormalizeRequired(nameof(moduleName), "Module name");
        CommandType = commandType;
        ValidatorType = validatorType;
    }

    public string ModuleName { get; }

    public Type CommandType { get; }

    public Type ValidatorType { get; }

    public ICommandValidator<TCommand> CreateValidator<TCommand>(
        IServiceProvider serviceProvider)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        if (CommandType != typeof(TCommand))
        {
            throw new InvalidOperationException(
                $"Validator '{ValidatorType.FullName}' is registered for command type "
                + $"'{CommandType.FullName}', not '{typeof(TCommand).FullName}'.");
        }

        object validator = serviceProvider.GetRequiredService(ValidatorType);
        return validator as ICommandValidator<TCommand>
            ?? throw new InvalidOperationException(
                $"Validator type '{ValidatorType.FullName}' does not implement "
                + $"'{typeof(ICommandValidator<TCommand>).FullName}'.");
    }
}
