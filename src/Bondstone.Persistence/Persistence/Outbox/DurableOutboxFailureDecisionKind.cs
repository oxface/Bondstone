namespace Bondstone.Persistence;

public enum DurableOutboxFailureDecisionKind
{
    Retry = 1,
    TerminalFailure = 2,
}
