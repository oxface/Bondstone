using Bondstone.Messaging;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Modules;

internal sealed class ModuleCommandOperationStatePipelineBehavior<TCommand>(
    IServiceProvider serviceProvider,
    DurableModuleOperationStateStoreResolver operationStateStoreResolver)
    : IModuleCommandPipelineBehavior<TCommand>
    where TCommand : ICommand
{
    private readonly IServiceProvider _serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    private readonly DurableModuleOperationStateStoreResolver _operationStateStoreResolver =
        operationStateStoreResolver ?? throw new ArgumentNullException(nameof(operationStateStoreResolver));

    public async ValueTask HandleAsync(
        TCommand command,
        ModuleCommandExecutionContext context,
        ModuleCommandPipelineNext next,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (context.DurableOperationId is not Guid durableOperationId)
        {
            await next(ct);
            return;
        }

        if (context.ReceiveInboxRecord is null)
        {
            throw new InvalidOperationException(
                $"Operation id '{durableOperationId}' was supplied without a receive inbox record. Operation completion is currently supported only for durable receive command execution.");
        }

        if (!context.Route.IsDurable)
        {
            throw new InvalidOperationException(
                $"Operation id '{durableOperationId}' was supplied for non-durable command route '{context.ModuleName}/{context.Route.CommandType.FullName}'.");
        }

        IDurableOperationStateStore operationStateStore =
            _operationStateStoreResolver.Resolve(
                context.ModuleName,
                durableOperationId);

        await next(ct);

        if (context.ReceiveInboxResult is null)
        {
            throw new InvalidOperationException(
                $"Durable command execution with operation id '{durableOperationId}' did not produce a receive inbox result.");
        }

        if (context.ReceiveInboxResult.Status != DurableInboxHandleStatus.Handled)
        {
            return;
        }

        TimeProvider timeProvider =
            _serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;

        await operationStateStore.SaveAsync(
            new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Completed,
                timeProvider.GetUtcNow(),
                context.DurableOperationResultPayload),
            ct);
    }

}
