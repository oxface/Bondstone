using Bondstone.Persistence;

namespace Bondstone.Modules;

public sealed class ModuleCommandExecutionContext(
    ModuleCommandRoute route,
    DurableInboxRecord? receiveInboxRecord = null)
{
    public ModuleCommandRoute Route { get; } =
        route ?? throw new ArgumentNullException(nameof(route));

    public DurableInboxRecord? ReceiveInboxRecord { get; } = receiveInboxRecord;

    public DurableInboxHandleResult? ReceiveInboxResult { get; private set; }

    public string ModuleName => Route.ModuleName;

    public string? MessageTypeName => Route.MessageTypeName;

    public string? HandlerIdentity => Route.HandlerIdentity;

    internal void SetReceiveInboxResult(DurableInboxHandleResult result)
    {
        ReceiveInboxResult = result ?? throw new ArgumentNullException(nameof(result));
    }
}
