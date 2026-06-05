namespace Bondstone.Persistence;

public enum DurableInboxRegistrationStatus
{
    Registered = 1,
    AlreadyReceived = 2,
    AlreadyProcessed = 3,
}
