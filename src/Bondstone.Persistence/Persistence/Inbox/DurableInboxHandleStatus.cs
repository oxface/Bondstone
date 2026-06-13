namespace Bondstone.Persistence;

public enum DurableInboxHandleStatus
{
    Handled = 1,
    AlreadyReceived = 2,
    AlreadyProcessed = 3,
}
