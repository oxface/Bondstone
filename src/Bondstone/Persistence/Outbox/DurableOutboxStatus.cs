namespace Bondstone.Persistence;

public enum DurableOutboxStatus
{
    Pending = 1,
    Processing = 2,
    Dispatched = 3,
    Failed = 4,
    TerminalFailed = 5,
}
