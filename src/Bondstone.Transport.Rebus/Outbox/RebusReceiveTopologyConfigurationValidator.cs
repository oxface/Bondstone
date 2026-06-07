using Bondstone.Configuration;
using Bondstone.Modules;
using Bondstone.Transport.Rebus.Inbox;

namespace Bondstone.Transport.Rebus.Outbox;

internal sealed class RebusReceiveTopologyConfigurationValidator(
    IReadOnlyCollection<RebusModuleReceiveEndpointBinding> receiveEndpointBindings)
    : IBondstoneConfigurationValidator
{
    public void Validate(BondstoneConfigurationValidationContext context)
    {
        foreach (RebusModuleReceiveEndpointBinding endpoint in receiveEndpointBindings)
        {
            foreach (string moduleName in endpoint.ModuleNames)
            {
                if (!context.ModulesByName.TryGetValue(
                    moduleName,
                    out BondstoneModuleRegistration? module))
                {
                    throw new InvalidOperationException(
                        $"Rebus receive endpoint '{endpoint.EndpointName}' accepts module '{moduleName}', but that module is not registered. Register module '{moduleName}' in AddBondstone or remove it from the Rebus receive endpoint.");
                }

                if (!module.UsesDurableMessaging)
                {
                    throw new InvalidOperationException(
                        $"Rebus receive endpoint '{endpoint.EndpointName}' accepts module '{moduleName}', but that module does not use durable messaging. Call UseDurableMessaging for module '{moduleName}'.");
                }

                if (!context.ModuleHasDurableCommandHandlers(moduleName))
                {
                    throw new InvalidOperationException(
                        $"Rebus receive endpoint '{endpoint.EndpointName}' accepts module '{moduleName}', but the module has no durable command handlers. Register at least one IDurableCommand handler for module '{moduleName}' or remove it from the Rebus receive endpoint.");
                }
            }
        }
    }
}
