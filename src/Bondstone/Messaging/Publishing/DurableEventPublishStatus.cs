namespace Bondstone.Messaging;

public enum DurableEventPublishStatus
{
    Accepted = 1,
    Rejected = 2,
    Duplicate = 3,
}
