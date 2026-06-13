using Testcontainers.PostgreSql;
using Xunit;

namespace Bondstone.Samples.Tests;

public sealed class PostgreSqlSampleFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("bondstone_samples")
        .Build();

    public string ConnectionString => $"{_container.GetConnectionString()};Pooling=false";

    public Task InitializeAsync()
    {
        return _container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _container.DisposeAsync().AsTask();
    }
}
