using Testcontainers.ServiceBus;
using Xunit;

namespace Bondstone.Transport.ServiceBus.Tests;

public sealed class ServiceBusFixture : IAsyncLifetime
{
    private readonly ServiceBusContainer _container =
        new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithAcceptLicenseAgreement(true)
            .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
