using System.Text.Json;
using Bondstone.Modules;
using Bondstone.Persistence;

namespace Bondstone.Messaging;

internal sealed class DurableCommandSender(
    IDurableOutboxWriter outboxWriter,
    IMessageTypeRegistry messageTypeRegistry,
    IModuleExecutionContextAccessor executionContextAccessor,
    TimeProvider? timeProvider = null)
    : IDurableCommandSender
{
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly IDurableOutboxWriter _outboxWriter =
        outboxWriter ?? throw new ArgumentNullException(nameof(outboxWriter));
    private readonly IMessageTypeRegistry _messageTypeRegistry =
        messageTypeRegistry ?? throw new ArgumentNullException(nameof(messageTypeRegistry));
    private readonly IModuleExecutionContextAccessor _executionContextAccessor =
        executionContextAccessor ?? throw new ArgumentNullException(nameof(executionContextAccessor));
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async ValueTask<DurableCommandSendResult> SendAsync<TCommand>(
        TCommand command,
        string targetModule,
        CancellationToken ct = default)
        where TCommand : IDurableCommand
    {
        return await SendAsync(
            command,
            targetModule,
            partitionKey: null,
            durableOperationId: null,
            traceContext: null,
            causationId: null,
            ct);
    }

    public async ValueTask<DurableCommandSendResult> SendAsync<TCommand>(
        TCommand command,
        string targetModule,
        string? partitionKey,
        Guid? durableOperationId = null,
        MessageTraceContext? traceContext = null,
        Guid? causationId = null,
        CancellationToken ct = default)
        where TCommand : IDurableCommand
    {
        ArgumentNullException.ThrowIfNull(command);

        ModuleExecutionContext executionContext =
            _executionContextAccessor.Current
            ?? throw new InvalidOperationException(
                "Durable command sending requires a current module execution context.");

        Guid messageId = Guid.NewGuid();
        string messageTypeName = _messageTypeRegistry.GetMessageTypeName<TCommand>();
        string payload = JsonSerializer.Serialize(command, JsonSerializerOptions);
        MessageTraceContext? capturedTraceContext =
            traceContext ?? MessageTraceContext.CaptureCurrent();

        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Command,
            messageTypeName,
            executionContext.ModuleName,
            targetModule,
            payload,
            _timeProvider.GetUtcNow(),
            durableOperationId,
            capturedTraceContext,
            causationId,
            partitionKey);

        await _outboxWriter.WriteAsync(envelope, ct);

        return new DurableCommandSendResult(
            messageId,
            durableOperationId,
            DurableCommandSendStatus.Accepted);
    }
}

