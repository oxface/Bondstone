using Bondstone.Messaging;

namespace Bondstone.Modules;

public interface IModuleEventSubscriberExecutor
{
    ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity,
        object integrationEvent,
        CancellationToken ct = default);

    ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity,
        object integrationEvent,
        ModuleEventSubscriberReceiveContext receiveContext,
        CancellationToken ct = default);

    ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync<TEvent>(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity,
        TEvent integrationEvent,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent;

    ValueTask<ModuleEventSubscriberExecutionResult> ExecuteAsync<TEvent>(
        string moduleName,
        string messageTypeName,
        string subscriberIdentity,
        TEvent integrationEvent,
        ModuleEventSubscriberReceiveContext receiveContext,
        CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
