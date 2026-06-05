namespace Bondstone.Persistence;

public enum DurableOutboxFailureDecisionKind
{
    Retry = 1,
    DeadLetter = 2,
}
