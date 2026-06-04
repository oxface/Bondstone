namespace Bondstone.Messaging;

/// <summary>
/// Accepts durable commands for asynchronous outbox delivery.
/// </summary>
public interface IDurableCommandSender
{
    ValueTask<DurableCommandSendResult> SendAsync<TCommand>(
        TCommand command,
        string targetModule,
        CancellationToken cancellationToken = default)
        where TCommand : IDurableCommand;

    ValueTask<DurableCommandSendResult> SendAsync<TCommand>(
        TCommand command,
        string targetModule,
        string? partitionKey,
        Guid? durableOperationId = null,
        MessageTraceContext? traceContext = null,
        Guid? causationId = null,
        CancellationToken cancellationToken = default)
        where TCommand : IDurableCommand;
}
