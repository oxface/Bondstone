namespace Bondstone.Persistence;

public interface IDurableIncomingInboxFailurePolicy
{
    DurableIncomingInboxFailureDecision DecideFailure(
        DurableIncomingInboxRecord record,
        string failureReason,
        DateTimeOffset failedAtUtc);
}
