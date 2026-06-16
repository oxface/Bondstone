using Bondstone.Modules;
using Bondstone.Persistence;

namespace Bondstone.Messaging;

internal sealed class DurableCommandSender(
    DurableModuleOutboxWriterResolver outboxWriterResolver,
    IMessageTypeRegistry messageTypeRegistry,
    IModuleExecutionContextAccessor executionContextAccessor,
    DurableModuleOperationStateStoreResolver operationStateStoreResolver,
    IDurablePayloadSerializer? payloadSerializer = null,
    TimeProvider? timeProvider = null)
    : IDurableCommandSender
{
    private readonly DurableModuleOutboxWriterResolver _outboxWriterResolver =
        outboxWriterResolver ?? throw new ArgumentNullException(nameof(outboxWriterResolver));
    private readonly IMessageTypeRegistry _messageTypeRegistry =
        messageTypeRegistry ?? throw new ArgumentNullException(nameof(messageTypeRegistry));
    private readonly IModuleExecutionContextAccessor _executionContextAccessor =
        executionContextAccessor ?? throw new ArgumentNullException(nameof(executionContextAccessor));
    private readonly DurableModuleOperationStateStoreResolver _operationStateStoreResolver =
        operationStateStoreResolver ?? throw new ArgumentNullException(nameof(operationStateStoreResolver));
    private readonly IDurablePayloadSerializer _payloadSerializer =
        payloadSerializer ?? new SystemTextJsonDurablePayloadSerializer();
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
        string payload = _payloadSerializer.Serialize(command);
        MessageTraceContext? capturedTraceContext =
            traceContext ?? MessageTraceContext.CaptureCurrent();

        DateTimeOffset createdAtUtc = _timeProvider.GetUtcNow();
        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Command,
            messageTypeName,
            executionContext.ModuleName,
            targetModule,
            payload,
            createdAtUtc,
            durableOperationId,
            capturedTraceContext,
            causationId,
            partitionKey);

        string sourceModule = executionContext.ModuleName;
        IDurableOutboxWriter outboxWriter = _outboxWriterResolver.Resolve(sourceModule);
        IDurableOperationStateStore? operationStateStore = null;
        if (durableOperationId is Guid operationId)
        {
            operationStateStore = _operationStateStoreResolver.Resolve(
                sourceModule,
                operationId);
        }

        await outboxWriter.WriteAsync(envelope, ct);

        if (durableOperationId is Guid operationIdToTrack)
        {
            await SavePendingOperationStateIfUnknownAsync(
                operationStateStore!,
                operationIdToTrack,
                createdAtUtc,
                ct);
        }

        DurableOperationHandle? operation = durableOperationId is Guid operationIdToReturn
            ? new DurableOperationHandle(
                operationIdToReturn,
                envelope.SourceModule,
                envelope.TargetModule!)
            : null;

        return new DurableCommandSendResult(
            messageId,
            operation,
            DurableCommandSendStatus.Accepted);
    }

    private static async ValueTask SavePendingOperationStateIfUnknownAsync(
        IDurableOperationStateStore operationStateStore,
        Guid durableOperationId,
        DateTimeOffset updatedAtUtc,
        CancellationToken ct)
    {
        DurableOperationState? existingState = await operationStateStore.GetStateAsync(
            durableOperationId,
            ct);

        if (existingState is not null)
        {
            return;
        }

        await operationStateStore.SaveAsync(
            new DurableOperationState(
                durableOperationId,
                DurableOperationStatus.Pending,
                updatedAtUtc),
            ct);
    }
}
