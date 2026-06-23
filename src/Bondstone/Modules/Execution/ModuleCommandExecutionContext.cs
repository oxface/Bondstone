using Bondstone.Persistence;

namespace Bondstone.Modules;

internal sealed class ModuleCommandExecutionContext(
    ModuleCommandRoute route,
    ModuleCommandReceiveContext? receiveContext = null)
    : ModuleRuntimeExecutionContextBase
{
    public ModuleCommandRoute Route { get; } =
        route ?? throw new ArgumentNullException(nameof(route));

    public ModuleCommandReceiveContext? ReceiveContext { get; } = receiveContext;

    public DurableInboxRecord? ReceiveInboxRecord => ReceiveContext?.InboxRecord;

    public Guid? DurableOperationId => ReceiveContext?.DurableOperationId;

    public DurableInboxHandleResult? ReceiveInboxResult { get; private set; }

    internal string? DurableOperationResultPayload { get; private set; }

    public override string ModuleName => Route.ModuleName;

    public string? MessageTypeName => Route.MessageTypeName;

    public string? HandlerIdentity => Route.HandlerIdentity;

    internal void SetReceiveInboxResult(DurableInboxHandleResult result)
    {
        ReceiveInboxResult = result ?? throw new ArgumentNullException(nameof(result));
    }

    internal void SetDurableOperationResultPayload(string payload)
    {
        DurableOperationResultPayload = payload ?? throw new ArgumentNullException(nameof(payload));
    }
}
