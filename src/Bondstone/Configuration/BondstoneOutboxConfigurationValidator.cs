using Bondstone.Diagnostics;

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
            throw new BondstoneSetupException(
                BondstoneSetupCodes.MissingOutboxPersistence,
                "Bondstone outbox dispatching requires an outbox persistence provider. "
                + "Register a persistence provider before enabling the dispatcher or worker.");
        }

        if (!outbox.HasTransport)
        {
            throw new BondstoneSetupException(
                BondstoneSetupCodes.MissingDispatcher,
                "Bondstone outbox dispatching requires an envelope dispatcher. "
                + "Register local transport or an app-owned durable envelope dispatcher before enabling the dispatcher or worker.");
        }
    }
}
