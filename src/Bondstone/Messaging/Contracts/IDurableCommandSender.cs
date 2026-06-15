namespace Bondstone.Messaging;

/// <summary>
/// Accepts durable commands for asynchronous outbox delivery.
/// </summary>
public interface IDurableCommandSender
{
    /// <summary>
    /// Stages a durable command in the current module's outbox for asynchronous delivery to a target module.
    /// </summary>
    /// <typeparam name="TCommand">The durable command type to send.</typeparam>
    /// <param name="command">The command instance to serialize and stage.</param>
    /// <param name="targetModule">The module that should receive and execute the command.</param>
    /// <param name="ct">A cancellation token for the send operation.</param>
    /// <returns>Accepted send metadata, including the durable send identifier and optional operation identifier.</returns>
    ValueTask<DurableCommandSendResult> SendAsync<TCommand>(
        TCommand command,
        string targetModule,
        CancellationToken ct = default)
        where TCommand : IDurableCommand;

    /// <summary>
    /// Stages a durable command in the current module's outbox with explicit routing, trace, and operation metadata.
    /// </summary>
    /// <typeparam name="TCommand">The durable command type to send.</typeparam>
    /// <param name="command">The command instance to serialize and stage.</param>
    /// <param name="targetModule">The module that should receive and execute the command.</param>
    /// <param name="partitionKey">An optional partition key used by persistence or transport adapters for ordering and routing.</param>
    /// <param name="durableOperationId">An optional durable operation identifier used to observe eventual operation state and result payloads.</param>
    /// <param name="traceContext">Optional distributed trace context to carry with the durable message.</param>
    /// <param name="causationId">Optional message causation identifier.</param>
    /// <param name="ct">A cancellation token for the send operation.</param>
    /// <returns>Accepted send metadata, including the durable send identifier and optional operation identifier.</returns>
    ValueTask<DurableCommandSendResult> SendAsync<TCommand>(
        TCommand command,
        string targetModule,
        string? partitionKey,
        Guid? durableOperationId = null,
        MessageTraceContext? traceContext = null,
        Guid? causationId = null,
        CancellationToken ct = default)
        where TCommand : IDurableCommand;
}
