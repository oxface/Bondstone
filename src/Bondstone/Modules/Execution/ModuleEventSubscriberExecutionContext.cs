using Bondstone.Persistence;

namespace Bondstone.Modules;

internal sealed class ModuleEventSubscriberExecutionContext(
    ModuleEventSubscriberRegistration subscriber,
    ModuleEventSubscriberReceiveContext? receiveContext = null)
    : ModuleRuntimeExecutionContextBase
{
    public ModuleEventSubscriberRegistration Subscriber { get; } =
        subscriber ?? throw new ArgumentNullException(nameof(subscriber));

    public ModuleEventSubscriberReceiveContext? ReceiveContext { get; } = receiveContext;

    public DurableInboxRecord? ReceiveInboxRecord => ReceiveContext?.InboxRecord;

    public Guid? DurableOperationId => ReceiveContext?.DurableOperationId;

    public DurableInboxHandleResult? ReceiveInboxResult { get; private set; }

    public override string ModuleName => Subscriber.ModuleName;

    public string MessageTypeName => Subscriber.MessageTypeName;

    public string SubscriberIdentity => Subscriber.SubscriberIdentity;

    internal void SetReceiveInboxResult(DurableInboxHandleResult result)
    {
        ReceiveInboxResult = result ?? throw new ArgumentNullException(nameof(result));
    }
}
