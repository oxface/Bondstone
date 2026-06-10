using System.ComponentModel;
using Bondstone.Utility;
using Microsoft.Extensions.DependencyInjection;

namespace Bondstone.Configuration;

public sealed class BondstoneOutboxBuilder
{
    private string? _persistenceProviderName;
    private string? _transportName;
    private string? _dispatcherName;
    private string? _workerName;

    internal BondstoneOutboxBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public bool HasPersistenceProvider => _persistenceProviderName is not null;

    public bool HasTransport => _transportName is not null;

    public bool HasDispatcher => _dispatcherName is not null;

    public bool HasWorker => _workerName is not null;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public BondstoneOutboxBuilder MarkPersistenceProvider(string providerName)
    {
        _persistenceProviderName = providerName.NormalizeRequired(nameof(providerName), "Capability name");
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public BondstoneOutboxBuilder MarkTransport(string transportName)
    {
        _transportName = transportName.NormalizeRequired(nameof(transportName), "Capability name");
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public BondstoneOutboxBuilder MarkDispatcher(string dispatcherName)
    {
        _dispatcherName = dispatcherName.NormalizeRequired(nameof(dispatcherName), "Capability name");
        return this;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public BondstoneOutboxBuilder MarkWorker(string workerName)
    {
        _workerName = workerName.NormalizeRequired(nameof(workerName), "Capability name");
        return this;
    }
}
