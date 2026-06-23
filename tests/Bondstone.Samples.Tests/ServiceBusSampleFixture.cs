using Testcontainers.ServiceBus;
using Xunit;

namespace Bondstone.Samples.Tests;

public sealed class ServiceBusSampleFixture : IAsyncLifetime
{
    private readonly ServiceBusContainer _container =
        new ServiceBusBuilder("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithAcceptLicenseAgreement(true)
            .WithConfig(Path.Combine(
                AppContext.BaseDirectory,
                "ServiceBusEmulatorConfig.json"))
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
