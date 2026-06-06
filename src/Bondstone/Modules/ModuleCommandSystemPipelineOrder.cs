namespace Bondstone.Modules;

public static class ModuleCommandSystemPipelineOrder
{
    public const int Transaction = 0;

    public const int ReceiveInbox = 50;

    public const int ExecutionContext = 100;
}
