using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Configuration;

public sealed class BondstoneBuilder
{
    internal BondstoneBuilder(IServiceCollection services)
    {
        Services = services;
        Outbox = new BondstoneOutboxBuilder(services);
    }

    public IServiceCollection Services { get; }

    public BondstoneOutboxBuilder Outbox { get; }

    internal void Validate()
    {
        Outbox.Validate();
    }
}
