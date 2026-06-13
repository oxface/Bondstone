namespace Bondstone.Configuration;

internal sealed class BondstoneOutboxConfigurationValidator(BondstoneOutboxBuilder outbox)
    : IBondstoneConfigurationValidator
{
    public void Validate(BondstoneConfigurationValidationContext context)
    {
        if (!outbox.HasDispatcher && !outbox.HasWorker)
        {
            return;
        }

        if (!outbox.HasPersistenceProvider)
        {
            throw new InvalidOperationException(
                "Bondstone outbox dispatching requires an outbox persistence provider. "
                + "Register a persistence provider before enabling the dispatcher or worker.");
        }

        if (!outbox.HasTransport)
        {
            throw new InvalidOperationException(
                "Bondstone outbox dispatching requires an outbox transport. "
                + "Register a transport before enabling the dispatcher or worker.");
        }
    }
}
