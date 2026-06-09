namespace Bondstone.Persistence;

public interface IDurableOutboxFailurePolicy
{
    DurableOutboxFailureDecision DecideFailure(
        DurableOutboxRecord record,
        string failureReason,
        DateTimeOffset failedAtUtc);
}
