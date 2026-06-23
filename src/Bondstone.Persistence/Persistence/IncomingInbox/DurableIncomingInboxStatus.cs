namespace Bondstone.Persistence;

public enum DurableIncomingInboxStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    RetryScheduled = 4,
    TerminalFailed = 5,
}
