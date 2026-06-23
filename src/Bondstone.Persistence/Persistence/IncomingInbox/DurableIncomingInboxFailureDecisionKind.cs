namespace Bondstone.Persistence;

public enum DurableIncomingInboxFailureDecisionKind
{
    Retry = 1,
    TerminalFailure = 2,
}
