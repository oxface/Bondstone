using Bondstone.Configuration;
using Bondstone.DomainEvents;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Persistence.EntityFrameworkCore.Postgres.Persistence;
using Bondstone.Messaging;
using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Postgres.Tests.DomainEvents;

public sealed class PostgreSqlDomainEventTransactionTests(PostgreSqlFixture fixture)
    : IClassFixture<PostgreSqlFixture>
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task ModuleCommands_WhenExternalTransactionIsJoined_DoesNotClearSourceOnLaterOwnedCommit()
    {
        await ResetExternalTransactionDomainEventDatabaseAsync();

        var services = new ServiceCollection();
        services.AddSingleton<ExternalTransactionDomainEventCapture>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UsePostgreSqlPersistence<ExternalTransactionDomainEventDbContext>(
                    fixture.ConnectionString);
                module.UseEntityFrameworkCoreDomainEventPersistence();
                module.Commands.RegisterHandler<
                    RaiseExternalTransactionDomainEventCommand,
                    RaiseExternalTransactionDomainEventCommandHandler>();
                module.Commands.RegisterHandler<
                    RaiseOwnedTransactionDomainEventCommand,
                    RaiseOwnedTransactionDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        ExternalTransactionDomainEventDbContext context = scope.ServiceProvider
            .GetRequiredService<ExternalTransactionDomainEventDbContext>();
        IModuleCommandExecutor executor = scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>();
        ExternalTransactionDomainEventCapture capture = scope.ServiceProvider
            .GetRequiredService<ExternalTransactionDomainEventCapture>();

        await using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction =
            await context.Database.BeginTransactionAsync())
        {
            await executor.ExecuteAsync(
                "fulfillment",
                new RaiseExternalTransactionDomainEventCommand("external"));

            await transaction.CommitAsync();
        }

        Assert.NotNull(capture.ExternalSource);
        Assert.Single(capture.ExternalSource.PendingDomainEvents);
        Assert.Equal(0, capture.ExternalSource.ClearCount);

        context.ChangeTracker.Clear();

        await executor.ExecuteAsync(
            "fulfillment",
            new RaiseOwnedTransactionDomainEventCommand("owned"));

        Assert.NotNull(capture.OwnedSource);
        Assert.Single(capture.ExternalSource.PendingDomainEvents);
        Assert.Equal(0, capture.ExternalSource.ClearCount);
        Assert.Empty(capture.OwnedSource.PendingDomainEvents);
        Assert.Equal(1, capture.OwnedSource.ClearCount);
    }

    private async Task ResetExternalTransactionDomainEventDatabaseAsync()
    {
        await using ExternalTransactionDomainEventDbContext context =
            CreateExternalTransactionDomainEventContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    private ExternalTransactionDomainEventDbContext CreateExternalTransactionDomainEventContext()
    {
        var options = new DbContextOptionsBuilder<ExternalTransactionDomainEventDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

        return new ExternalTransactionDomainEventDbContext(options);
    }

    public sealed record RaiseExternalTransactionDomainEventCommand(string Id) : ICommand;

    public sealed record RaiseOwnedTransactionDomainEventCommand(string Id) : ICommand;

    public sealed class RaiseExternalTransactionDomainEventCommandHandler(
        ExternalTransactionDomainEventDbContext context,
        ExternalTransactionDomainEventCapture capture)
        : ICommandHandler<RaiseExternalTransactionDomainEventCommand>
    {
        public ValueTask HandleAsync(
            RaiseExternalTransactionDomainEventCommand command,
            CancellationToken ct = default)
        {
            ExternalTransactionDomainEventSource source =
                ExternalTransactionDomainEventSource.Raise(command.Id);
            capture.ExternalSource = source;
            context.Sources.Add(source);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class RaiseOwnedTransactionDomainEventCommandHandler(
        ExternalTransactionDomainEventDbContext context,
        ExternalTransactionDomainEventCapture capture)
        : ICommandHandler<RaiseOwnedTransactionDomainEventCommand>
    {
        public ValueTask HandleAsync(
            RaiseOwnedTransactionDomainEventCommand command,
            CancellationToken ct = default)
        {
            ExternalTransactionDomainEventSource source =
                ExternalTransactionDomainEventSource.Raise(command.Id);
            capture.OwnedSource = source;
            context.Sources.Add(source);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ExternalTransactionDomainEventCapture
    {
        public ExternalTransactionDomainEventSource? ExternalSource { get; set; }

        public ExternalTransactionDomainEventSource? OwnedSource { get; set; }
    }

    public sealed class ExternalTransactionDomainEventSource : IDomainEventSource
    {
        private readonly List<IDomainEvent> _pendingDomainEvents = [];

        public string Id { get; set; } = string.Empty;

        public int ClearCount { get; private set; }

        public IReadOnlyCollection<IDomainEvent> PendingDomainEvents => _pendingDomainEvents;

        public static ExternalTransactionDomainEventSource Raise(string id)
        {
            var source = new ExternalTransactionDomainEventSource
            {
                Id = id,
            };
            source._pendingDomainEvents.Add(new ExternalTransactionDomainEvent(id));
            return source;
        }

        public void ClearPendingDomainEvents()
        {
            ClearCount++;
            _pendingDomainEvents.Clear();
        }
    }

    [DomainEventIdentity("fulfillment.external-transaction-test.v1")]
    public sealed record ExternalTransactionDomainEvent(string Id) : IDomainEvent;

    public sealed class ExternalTransactionDomainEventDbContext(
        DbContextOptions<ExternalTransactionDomainEventDbContext> options)
        : DbContext(options)
    {
        public DbSet<ExternalTransactionDomainEventSource> Sources => Set<ExternalTransactionDomainEventSource>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstoneDomainEvents();
            modelBuilder.Entity<ExternalTransactionDomainEventSource>(
                entity =>
                {
                    entity.HasKey(source => source.Id);
                    entity.Ignore(source => source.PendingDomainEvents);
                    entity.Ignore(source => source.ClearCount);
                });
        }
    }
}
