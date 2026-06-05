namespace Bondstone.Persistence;

public interface IDurableInboxRegistrar
{
    ValueTask<DurableInboxRegistrationResult> RegisterAsync(
        DurableInboxRecord record,
        CancellationToken cancellationToken = default);
}
