using Bondstone.Configuration;
using Bondstone.DomainEvents;
using Bondstone.Persistence.EntityFrameworkCore.DomainEvents;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Messaging;
using Bondstone.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.DomainEvents;

public sealed class EntityFrameworkCoreDomainEventPersistenceTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenEfBackedModuleOptsIn_CollectsAndClearsPendingDomainEvents()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventCapture>();
        services.AddDbContext<DomainEventTestDbContext>(options =>
            UseInMemory(options, databaseName));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<DomainEventTestDbContext>();
                module.UseEntityFrameworkCoreDomainEventPersistence();
                module.Commands.RegisterHandler<RaiseDomainEventCommand, RaiseDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new RaiseDomainEventCommand("A-100"));
        }

        DomainEventCapture capture = serviceProvider.GetRequiredService<DomainEventCapture>();
        Assert.NotNull(capture.Source);
        Assert.Empty(capture.Source.PendingDomainEvents);
        Assert.Equal(1, capture.Source.ClearCount);
        Assert.Equal(["save", "clear"], capture.Calls);

        using IServiceScope verificationScope = serviceProvider.CreateScope();
        DomainEventTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<DomainEventTestDbContext>();
        DomainEventRecordEntity record = await context.Set<DomainEventRecordEntity>().SingleAsync();

        Assert.Equal("fulfillment", record.ModuleName);
        Assert.Equal("fulfillment.inventory-reserved.v1", record.DomainEventName);
        Assert.Contains("\"inventoryId\":\"A-100\"", record.Payload, StringComparison.Ordinal);
        Assert.Contains(nameof(InventoryReservedDomainEvent), record.PayloadTypeName, StringComparison.Ordinal);
        Assert.Equal(record.CapturedAtUtc, record.OccurredAtUtc);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenApplicationBehaviorRaisesDomainEvent_StagesBeforeSave()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventCapture>();
        services.AddScoped<
            IModuleCommandPipelineBehavior<RaiseLoggedDomainEventCommand>,
            LoggedDomainEventCommandBehavior>();
        services.AddDbContext<DomainEventTestDbContext>(options =>
            UseInMemory(options, databaseName));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<DomainEventTestDbContext>();
                module.UseEntityFrameworkCoreDomainEventPersistence();
                module.Commands.RegisterHandler<
                    RaiseLoggedDomainEventCommand,
                    RaiseLoggedDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        DomainEventCapture capture = serviceProvider.GetRequiredService<DomainEventCapture>();
        capture.RecordStagedDomainEventsAtSave = true;

        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new RaiseLoggedDomainEventCommand("A-100"));
        }

        Assert.Equal(
            [
                "application-before",
                "handler",
                "application-after",
                "save:domain-events:1",
                "clear",
            ],
            capture.Calls);
        using IServiceScope verificationScope = serviceProvider.CreateScope();
        DomainEventTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<DomainEventTestDbContext>();
        DomainEventRecordEntity record = await context.Set<DomainEventRecordEntity>().SingleAsync();

        Assert.Equal("fulfillment.inventory-reserved.v1", record.DomainEventName);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenEfBackedModuleDoesNotOptIn_DoesNotCollectOrClearDomainEvents()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventCapture>();
        services.AddDbContext<DomainEventTestDbContext>(options =>
            UseInMemory(options, databaseName));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<DomainEventTestDbContext>();
                module.Commands.RegisterHandler<RaiseDomainEventCommand, RaiseDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new RaiseDomainEventCommand("A-100"));
        }

        DomainEventCapture capture = serviceProvider.GetRequiredService<DomainEventCapture>();
        Assert.NotNull(capture.Source);
        Assert.Single(capture.Source.PendingDomainEvents);
        Assert.Equal(0, capture.Source.ClearCount);
        Assert.Equal(["save"], capture.Calls);

        using IServiceScope verificationScope = serviceProvider.CreateScope();
        DomainEventTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<DomainEventTestDbContext>();
        Assert.Empty(await context.Set<DomainEventRecordEntity>().ToArrayAsync());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenAnotherEfBackedModuleOptsIn_DoesNotCollectOrClearDomainEvents()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventCapture>();
        services.AddDbContext<DomainEventTestDbContext>(options =>
            UseInMemory(options, databaseName));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseEntityFrameworkCorePersistence<DomainEventTestDbContext>();
                module.UseEntityFrameworkCoreDomainEventPersistence();
            });
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<DomainEventTestDbContext>();
                module.Commands.RegisterHandler<RaiseDomainEventCommand, RaiseDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new RaiseDomainEventCommand("A-100"));
        }

        DomainEventCapture capture = serviceProvider.GetRequiredService<DomainEventCapture>();
        Assert.NotNull(capture.Source);
        Assert.Single(capture.Source.PendingDomainEvents);
        Assert.Equal(0, capture.Source.ClearCount);
        Assert.Equal(["save"], capture.Calls);

        using IServiceScope verificationScope = serviceProvider.CreateScope();
        DomainEventTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<DomainEventTestDbContext>();
        Assert.Empty(await context.Set<DomainEventRecordEntity>().ToArrayAsync());
    }

    [Fact]
    [Trait("Category", "Application")]
    public void ModuleCommands_WhenModuleOptsInButIsNotEfBacked_FailsAtStartup()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventCapture>();
        services.AddDbContext<DomainEventTestDbContext>(options =>
            UseInMemory(options, databaseName));

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.UseEntityFrameworkCoreDomainEventPersistence();
                    module.Commands.RegisterHandler<RaiseDomainEventCommand, RaiseDomainEventCommandHandler>();
                });
            }));

        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Bondstone.Persistence.EntityFrameworkCore.DomainEvents.Command",
            exception.Message,
            StringComparison.Ordinal);
        Assert.Contains("required persistence declaration", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Application")]
    public void ModuleCommands_WhenHandlerlessModuleOptsInButIsNotEfBacked_FailsAtStartup()
    {
        var services = new ServiceCollection();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => services.AddBondstone(bondstone =>
            {
                bondstone.Module("fulfillment", module =>
                {
                    module.UseEntityFrameworkCoreDomainEventPersistence();
                });
            }));

        Assert.Contains("fulfillment", exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Bondstone.Persistence.EntityFrameworkCore.DomainEvents.Command",
            exception.Message,
            StringComparison.Ordinal);
        Assert.Contains("required persistence declaration", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenModuleOptsInButDomainEventMappingIsMissing_FailsWithExplicitMappingMessage()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventCapture>();
        services.AddDbContext<MissingDomainEventMappingDbContext>(options =>
            UseInMemory(options, databaseName));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<MissingDomainEventMappingDbContext>();
                module.UseEntityFrameworkCoreDomainEventPersistence();
                module.Commands.RegisterHandler<
                    RaiseDomainEventCommand,
                    MissingMappingRaiseDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new RaiseDomainEventCommand("A-100")));

        Assert.Contains("ApplyBondstoneDomainEvents()", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyBondstonePersistence()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenSaveFails_DoesNotClearPendingDomainEvents()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventCapture>();
        services.AddDbContext<FailingSaveDomainEventTestDbContext>(options =>
            UseInMemory(options, databaseName));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<FailingSaveDomainEventTestDbContext>();
                module.UseEntityFrameworkCoreDomainEventPersistence();
                module.Commands.RegisterHandler<RaiseDomainEventCommand, FailingSaveRaiseDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new RaiseDomainEventCommand("A-100")));

        DomainEventCapture capture = serviceProvider.GetRequiredService<DomainEventCapture>();
        Assert.NotNull(capture.Source);
        Assert.Single(capture.Source.PendingDomainEvents);
        Assert.Equal(0, capture.Source.ClearCount);
        Assert.Equal(["save"], capture.Calls);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenCollectionFails_DoesNotClearPendingDomainEvents()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventCapture>();
        services.AddDbContext<DomainEventTestDbContext>(options =>
            UseInMemory(options, databaseName));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<DomainEventTestDbContext>();
                module.UseEntityFrameworkCoreDomainEventPersistence();
                module.Commands.RegisterHandler<RaiseUnnamedDomainEventCommand, RaiseUnnamedDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new RaiseUnnamedDomainEventCommand("A-100")));

        Assert.Contains(nameof(DomainEventIdentityAttribute), exception.Message, StringComparison.Ordinal);
        DomainEventCapture capture = serviceProvider.GetRequiredService<DomainEventCapture>();
        Assert.NotNull(capture.Source);
        Assert.Single(capture.Source.PendingDomainEvents);
        Assert.Equal(0, capture.Source.ClearCount);
        Assert.Empty(capture.Calls);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleEventSubscribers_WhenEfBackedModuleOptsIn_CollectsAndClearsPendingDomainEvents()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventCapture>();
        services.AddDbContext<DomainEventDurableTestDbContext>(options =>
            UseInMemory(options, databaseName));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<DomainEventDurableTestDbContext>();
                module.UseEntityFrameworkCoreDomainEventPersistence();
                module.Events.RegisterSubscriber<
                    DomainEventSourceIntegrationEvent,
                    DomainEventSourceIntegrationEventHandler>("fulfillment.domain-source.v1");
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleEventSubscriberExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    "fulfillment.domain-source.v1",
                    "fulfillment.domain-source.v1",
                    new DomainEventSourceIntegrationEvent("E-100"));
        }

        DomainEventCapture capture = serviceProvider.GetRequiredService<DomainEventCapture>();
        Assert.NotNull(capture.Source);
        Assert.Empty(capture.Source.PendingDomainEvents);
        Assert.Equal(1, capture.Source.ClearCount);

        using IServiceScope verificationScope = serviceProvider.CreateScope();
        DomainEventDurableTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<DomainEventDurableTestDbContext>();
        DomainEventRecordEntity record = await context.Set<DomainEventRecordEntity>().SingleAsync();

        Assert.Equal("fulfillment", record.ModuleName);
        Assert.Equal("fulfillment.inventory-reserved.v1", record.DomainEventName);
        Assert.Contains("\"inventoryId\":\"E-100\"", record.Payload, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ApplyBondstoneDomainEvents_ConfiguresOnlyDomainEventEntity()
    {
        var modelBuilder = new ModelBuilder();

        modelBuilder.ApplyBondstoneDomainEvents("bondstone");

        Microsoft.EntityFrameworkCore.Metadata.IMutableModel model = modelBuilder.Model;
        Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType = model
            .FindEntityType(typeof(DomainEventRecordEntity))
            ?? throw new InvalidOperationException("Domain event entity type was not configured.");

        Assert.Equal("bondstone", entityType.GetSchema());
        Assert.Equal(DomainEventRecordEntityConfiguration.TableName, entityType.GetTableName());
        Assert.Equal(
            DomainEventRecordEntityConfiguration.PrimaryKeyName,
            entityType.FindPrimaryKey()!.GetName());
    }

    private static void UseInMemory(
        DbContextOptionsBuilder options,
        string databaseName)
    {
        options
            .UseInMemoryDatabase(databaseName)
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
    }

    public sealed record RaiseDomainEventCommand(string Id) : ICommand;

    public sealed record RaiseLoggedDomainEventCommand(string Id) : ICommand;

    public sealed record RaiseUnnamedDomainEventCommand(string Id) : ICommand;

    [IntegrationEventIdentity("fulfillment.domain-source.v1")]
    public sealed record DomainEventSourceIntegrationEvent(string Id) : IIntegrationEvent;

    public sealed class RaiseDomainEventCommandHandler(
        DomainEventTestDbContext context,
        DomainEventCapture capture)
        : ICommandHandler<RaiseDomainEventCommand>
    {
        public ValueTask HandleAsync(
            RaiseDomainEventCommand command,
            CancellationToken ct = default)
        {
            DomainEventAggregateEntity source = DomainEventAggregateEntity.Reserve(
                command.Id,
                () => capture.Calls.Add("clear"));
            capture.Source = source;
            context.Aggregates.Add(source);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class RaiseLoggedDomainEventCommandHandler(
        DomainEventTestDbContext context,
        DomainEventCapture capture)
        : ICommandHandler<RaiseLoggedDomainEventCommand>
    {
        public ValueTask HandleAsync(
            RaiseLoggedDomainEventCommand command,
            CancellationToken ct = default)
        {
            capture.Calls.Add("handler");
            DomainEventAggregateEntity source = DomainEventAggregateEntity.Reserve(
                command.Id,
                () => capture.Calls.Add("clear"));
            capture.Source = source;
            context.Aggregates.Add(source);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class LoggedDomainEventCommandBehavior(DomainEventCapture capture)
        : IModuleCommandPipelineBehavior<RaiseLoggedDomainEventCommand>
    {
        public async ValueTask HandleAsync(
            RaiseLoggedDomainEventCommand command,
            ModuleCommandExecutionContext context,
            ModuleCommandPipelineNext next,
            CancellationToken ct = default)
        {
            capture.Calls.Add("application-before");
            await next(ct);
            capture.Calls.Add("application-after");
        }
    }

    public sealed class FailingSaveRaiseDomainEventCommandHandler(
        FailingSaveDomainEventTestDbContext context,
        DomainEventCapture capture)
        : ICommandHandler<RaiseDomainEventCommand>
    {
        public ValueTask HandleAsync(
            RaiseDomainEventCommand command,
            CancellationToken ct = default)
        {
            DomainEventAggregateEntity source = DomainEventAggregateEntity.Reserve(
                command.Id,
                () => capture.Calls.Add("clear"));
            capture.Source = source;
            context.Aggregates.Add(source);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class RaiseUnnamedDomainEventCommandHandler(
        DomainEventTestDbContext context,
        DomainEventCapture capture)
        : ICommandHandler<RaiseUnnamedDomainEventCommand>
    {
        public ValueTask HandleAsync(
            RaiseUnnamedDomainEventCommand command,
            CancellationToken ct = default)
        {
            DomainEventAggregateEntity source = DomainEventAggregateEntity.RaiseUnnamed(
                command.Id,
                () => capture.Calls.Add("clear"));
            capture.Source = source;
            context.Aggregates.Add(source);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class MissingMappingRaiseDomainEventCommandHandler(
        MissingDomainEventMappingDbContext context,
        DomainEventCapture capture)
        : ICommandHandler<RaiseDomainEventCommand>
    {
        public ValueTask HandleAsync(
            RaiseDomainEventCommand command,
            CancellationToken ct = default)
        {
            DomainEventAggregateEntity source = DomainEventAggregateEntity.Reserve(command.Id);
            capture.Source = source;
            context.Aggregates.Add(source);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class DomainEventSourceIntegrationEventHandler(
        DomainEventDurableTestDbContext context,
        DomainEventCapture capture)
        : IIntegrationEventHandler<DomainEventSourceIntegrationEvent>
    {
        public ValueTask HandleAsync(
            DomainEventSourceIntegrationEvent integrationEvent,
            CancellationToken ct = default)
        {
            DomainEventAggregateEntity source = DomainEventAggregateEntity.Reserve(
                integrationEvent.Id,
                () => capture.Calls.Add("clear"));
            capture.Source = source;
            context.Aggregates.Add(source);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class DomainEventCapture
    {
        public List<string> Calls { get; } = [];

        public DomainEventAggregateEntity? Source { get; set; }

        public bool RecordStagedDomainEventsAtSave { get; set; }
    }

    public sealed class DomainEventAggregateEntity : IDomainEventSource
    {
        private readonly List<IDomainEvent> _pendingDomainEvents = [];
        private Action? _onClear;

        public string Id { get; set; } = string.Empty;

        public int ClearCount { get; private set; }

        public IReadOnlyCollection<IDomainEvent> PendingDomainEvents => _pendingDomainEvents;

        public static DomainEventAggregateEntity Reserve(
            string inventoryId,
            Action? onClear = null)
        {
            var source = new DomainEventAggregateEntity
            {
                Id = inventoryId,
                _onClear = onClear,
            };
            source._pendingDomainEvents.Add(new InventoryReservedDomainEvent(inventoryId));
            return source;
        }

        public static DomainEventAggregateEntity RaiseUnnamed(
            string inventoryId,
            Action? onClear = null)
        {
            var source = new DomainEventAggregateEntity
            {
                Id = inventoryId,
                _onClear = onClear,
            };
            source._pendingDomainEvents.Add(new UnnamedDomainEvent(inventoryId));
            return source;
        }

        public void ClearPendingDomainEvents()
        {
            ClearCount++;
            _onClear?.Invoke();
            _pendingDomainEvents.Clear();
        }
    }

    [DomainEventIdentity("fulfillment.inventory-reserved.v1")]
    public sealed record InventoryReservedDomainEvent(string InventoryId) : IDomainEvent;

    public sealed record UnnamedDomainEvent(string InventoryId) : IDomainEvent;

    public sealed class DomainEventTestDbContext(
        DbContextOptions<DomainEventTestDbContext> options,
        DomainEventCapture capture)
        : DbContext(options)
    {
        private readonly DomainEventCapture _capture = capture;

        public DbSet<DomainEventAggregateEntity> Aggregates => Set<DomainEventAggregateEntity>();

        public override async Task<int> SaveChangesAsync(
            CancellationToken ct = default)
        {
            if (_capture.RecordStagedDomainEventsAtSave)
            {
                int stagedDomainEventRecords = ChangeTracker
                    .Entries<DomainEventRecordEntity>()
                    .Count(entry => entry.State == EntityState.Added);
                _capture.Calls.Add($"save:domain-events:{stagedDomainEventRecords}");
            }
            else
            {
                _capture.Calls.Add("save");
            }

            return await base.SaveChangesAsync(ct);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureDomainEventModel(modelBuilder);
        }
    }

    public sealed class FailingSaveDomainEventTestDbContext(
        DbContextOptions<FailingSaveDomainEventTestDbContext> options,
        DomainEventCapture capture)
        : DbContext(options)
    {
        private readonly DomainEventCapture _capture = capture;

        public DbSet<DomainEventAggregateEntity> Aggregates => Set<DomainEventAggregateEntity>();

        public override Task<int> SaveChangesAsync(
            CancellationToken ct = default)
        {
            _capture.Calls.Add("save");
            throw new InvalidOperationException("Save failed.");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureDomainEventModel(modelBuilder);
        }
    }

    public sealed class MissingDomainEventMappingDbContext(
        DbContextOptions<MissingDomainEventMappingDbContext> options)
        : DbContext(options)
    {
        public DbSet<DomainEventAggregateEntity> Aggregates => Set<DomainEventAggregateEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DomainEventAggregateEntity>(
                entity =>
                {
                    entity.HasKey(source => source.Id);
                    entity.Ignore(source => source.PendingDomainEvents);
                    entity.Ignore(source => source.ClearCount);
                });
        }
    }

    public sealed class DomainEventDurableTestDbContext(
        DbContextOptions<DomainEventDurableTestDbContext> options)
        : DbContext(options)
    {
        public DbSet<DomainEventAggregateEntity> Aggregates => Set<DomainEventAggregateEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstoneOutbox();
            modelBuilder.ApplyBondstoneInbox();
            ConfigureDomainEventModel(modelBuilder);
        }
    }

    private static void ConfigureDomainEventModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyBondstoneDomainEvents();
        modelBuilder.Entity<DomainEventAggregateEntity>(
            entity =>
            {
                entity.HasKey(source => source.Id);
                entity.Ignore(source => source.PendingDomainEvents);
                entity.Ignore(source => source.ClearCount);
            });
    }
}
