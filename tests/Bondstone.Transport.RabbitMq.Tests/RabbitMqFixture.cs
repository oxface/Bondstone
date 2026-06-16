using Testcontainers.RabbitMq;
using Xunit;

namespace Bondstone.Transport.RabbitMq.Tests;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container =
        new RabbitMqBuilder("rabbitmq:4.1-alpine")
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
