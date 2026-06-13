namespace Bondstone.Messaging;

public enum DurableCommandSendStatus
{
    Accepted = 1,
    Rejected = 2,
    Duplicate = 3,
}
