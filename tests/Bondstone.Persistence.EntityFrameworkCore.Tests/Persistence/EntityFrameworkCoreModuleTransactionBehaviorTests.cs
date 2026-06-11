using Bondstone.Configuration;
using Bondstone.Persistence.EntityFrameworkCore.Operations;
using Bondstone.Persistence.EntityFrameworkCore.Persistence;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Persistence.EntityFrameworkCore.Tests.Persistence;

public sealed class EntityFrameworkCoreModuleTransactionBehaviorTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenModuleUsesEntityFrameworkCorePersistence_SavesChangesAfterHandler()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddDbContext<ModuleTransactionTestDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<ModuleTransactionTestDbContext>();
                module.Commands.RegisterValidator<StoreHandledCommand, StoreHandledCommandValidator>();
                module.Commands.RegisterHandler<StoreHandledCommand, StoreHandledCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new StoreHandledCommand("A-100"));
        }

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(["validate:A-100", "handle:A-100", "save"], log.Calls);

        using IServiceScope verificationScope = serviceProvider.CreateScope();
        ModuleTransactionTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<ModuleTransactionTestDbContext>();
        HandledCommandEntity entity = await context.HandledCommands.SingleAsync();
        Assert.Equal("A-100", entity.Id);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenModuleDoesNotUsePersistence_DoesNotSaveChanges()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddDbContext<ModuleTransactionTestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseEntityFrameworkCorePersistence<ModuleTransactionTestDbContext>();
            });
            bondstone.Module("fulfillment", module =>
            {
                module.Commands.RegisterHandler<StoreHandledCommand, StoreHandledCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new StoreHandledCommand("A-100"));
        }

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(["handle:A-100"], log.Calls);

        using IServiceScope verificationScope = serviceProvider.CreateScope();
        ModuleTransactionTestDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<ModuleTransactionTestDbContext>();
        Assert.Empty(await context.HandledCommands.ToArrayAsync());
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenDurableMessagingDbContextIsMissingOutboxMapping_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddDbContext<InboxOnlyMappingDbContext>(options =>
            options
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<InboxOnlyMappingDbContext>();
                module.Commands.RegisterHandler<StoreHandledCommand, LoggingCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new StoreHandledCommand("A-100")));

        Assert.Contains("missing required Bondstone EF Core mappings: outbox", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ApplyBondstoneOutbox()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenDurableMessagingDbContextIsMissingInboxMapping_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddDbContext<OutboxOnlyMappingDbContext>(options =>
            options
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<OutboxOnlyMappingDbContext>();
                module.Commands.RegisterHandler<StoreHandledCommand, LoggingCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new StoreHandledCommand("A-100")));

        Assert.Contains("missing required Bondstone EF Core mappings: inbox", exception.Message, StringComparison.Ordinal);
        Assert.Contains("ApplyBondstoneInbox()", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenDurableMessagingDbContextMapsOutboxAndInbox_AllowsExecution()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddDbContext<OutboxInboxMappingDbContext>(options =>
            options
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<OutboxInboxMappingDbContext>();
                module.Commands.RegisterHandler<StoreHandledCommand, LoggingCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "fulfillment",
                new StoreHandledCommand("A-100"));

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();
        Assert.Equal(["handle:A-100"], log.Calls);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenReceiveHasOperationId_SavesCompletedOperationStateInModuleTransaction()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        Guid durableOperationId = Guid.Parse("19a598fd-c659-4937-bdea-f4c7eb464766");
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(
            new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled));
        services.AddDbContext<FullDurableMappingDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        services.AddBondstoneEntityFrameworkCorePersistence<FullDurableMappingDbContext>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<FullDurableMappingDbContext>();
                module.Commands.RegisterHandler<
                    StoreDurableHandledCommand,
                    StoreDurableHandledCommandHandler>();
            });
        });

        var inboxRecord = new DurableInboxRecord(
            new DurableInboxMessageKey(
                Guid.Parse("3f1a9e26-75d4-4a7d-bb48-ae453f5e5e02"),
                "fulfillment",
                "fulfillment.store-handled.v1"),
            DateTimeOffset.Parse("2026-06-08T12:00:00+00:00"));

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleCommandExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    new StoreDurableHandledCommand("A-100"),
                    new ModuleCommandReceiveContext(
                        inboxRecord,
                        durableOperationId));
        }

        using IServiceScope verificationScope = serviceProvider.CreateScope();
        FullDurableMappingDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<FullDurableMappingDbContext>();
        HandledCommandEntity handledCommand = await context.HandledCommands.SingleAsync();
        OperationStateEntity operationState = await context
            .Set<OperationStateEntity>()
            .SingleAsync();

        Assert.Equal("A-100", handledCommand.Id);
        Assert.Equal(durableOperationId, operationState.DurableOperationId);
        Assert.Equal(DurableOperationStatus.Completed, operationState.Status);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleEventSubscribers_WhenModuleUsesEntityFrameworkCorePersistence_SavesChangesAfterHandler()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddDbContext<FullDurableMappingDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<FullDurableMappingDbContext>();
                module.Events.RegisterSubscriber<
                    StoreDurableHandledEvent,
                    StoreDurableHandledEventHandler>("fulfillment.store-event.v1");
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            await scope.ServiceProvider
                .GetRequiredService<IModuleEventSubscriberExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    "fulfillment.store-event.v1",
                    "fulfillment.store-event.v1",
                    new StoreDurableHandledEvent("E-100"));
        }

        using IServiceScope verificationScope = serviceProvider.CreateScope();
        FullDurableMappingDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<FullDurableMappingDbContext>();
        HandledCommandEntity handledEvent = await context.HandledCommands.SingleAsync();

        Assert.Equal("event:E-100", handledEvent.Id);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleEventSubscribers_WhenReceiveContextExists_SavesHandlerStateInModuleTransaction()
    {
        string databaseName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddSingleton<IDurableInboxHandlerExecutor>(
            new CapturingInboxHandlerExecutor(DurableInboxHandleStatus.Handled));
        services.AddDbContext<FullDurableMappingDbContext>(options =>
            options
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(warnings =>
                    warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UseEntityFrameworkCorePersistence<FullDurableMappingDbContext>();
                module.Events.RegisterSubscriber<
                    StoreDurableHandledEvent,
                    StoreDurableHandledEventHandler>("fulfillment.store-event.v1");
            });
        });

        var inboxRecord = new DurableInboxRecord(
            DurableInboxMessageKey.ForEventSubscriber(
                Guid.Parse("1e7e4d66-a402-401f-9324-d2ac7efa7741"),
                "fulfillment",
                "fulfillment.store-event.v1"),
            DateTimeOffset.Parse("2026-06-09T12:00:00+00:00"));

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using (IServiceScope scope = serviceProvider.CreateScope())
        {
            ModuleEventSubscriberExecutionResult result = await scope.ServiceProvider
                .GetRequiredService<IModuleEventSubscriberExecutor>()
                .ExecuteAsync(
                    "fulfillment",
                    "fulfillment.store-event.v1",
                    "fulfillment.store-event.v1",
                    new StoreDurableHandledEvent("E-100"),
                    new ModuleEventSubscriberReceiveContext(inboxRecord));

            Assert.Equal(DurableInboxHandleStatus.Handled, result.ReceiveInboxResult?.Status);
        }

        using IServiceScope verificationScope = serviceProvider.CreateScope();
        FullDurableMappingDbContext context =
            verificationScope.ServiceProvider.GetRequiredService<FullDurableMappingDbContext>();
        HandledCommandEntity handledEvent = await context.HandledCommands.SingleAsync();

        Assert.Equal("event:E-100", handledEvent.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseEntityFrameworkCorePersistence_WhenCalled_RecordsModulePersistenceMetadata()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<ModuleTransactionTestDbContext>();
            });
        });

        using ServiceProvider serviceProvider = services.BuildServiceProvider();
        IBondstoneModuleRegistry registry =
            serviceProvider.GetRequiredService<IBondstoneModuleRegistry>();

        BondstoneModuleRegistration module = registry.GetModule("fulfillment");

        Assert.True(module.UsesPersistence);
        Assert.Equal("EntityFrameworkCore", module.PersistenceProviderName);
        Assert.Equal(typeof(ModuleTransactionTestDbContext), module.PersistenceContextType);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UseEntityFrameworkCorePersistence_WhenCalled_RegistersTransactionBehaviorAsSystemBehavior()
    {
        var services = new ServiceCollection();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseEntityFrameworkCorePersistence<ModuleTransactionTestDbContext>();
            });
        });

        Type[] commandSystemBehaviorTypes = services
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(IModuleCommandSystemPipelineBehavior<>))
            .Select(static descriptor => descriptor.ImplementationType!)
            .ToArray();
        Type[] eventSystemBehaviorTypes = services
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(IModuleEventSubscriberSystemPipelineBehavior<>))
            .Select(static descriptor => descriptor.ImplementationType!)
            .ToArray();
        Type[] applicationBehaviorTypes = services
            .Where(static descriptor =>
                descriptor.ServiceType == typeof(IModuleCommandPipelineBehavior<>))
            .Select(static descriptor => descriptor.ImplementationType!)
            .ToArray();

        Assert.Contains(
            commandSystemBehaviorTypes,
            type => type.FullName == "Bondstone.Persistence.EntityFrameworkCore.Persistence.EntityFrameworkCoreModuleTransactionBehavior`1");
        Assert.Contains(
            commandSystemBehaviorTypes,
            type => type.FullName == "Bondstone.Modules.ModuleExecutionContextPipelineBehavior`1");
        Assert.Contains(
            eventSystemBehaviorTypes,
            type => type.FullName == "Bondstone.Persistence.EntityFrameworkCore.Persistence.EntityFrameworkCoreModuleEventSubscriberTransactionBehavior`1");
        Assert.Contains(
            eventSystemBehaviorTypes,
            type => type.FullName == "Bondstone.Modules.ModuleEventSubscriberExecutionContextPipelineBehavior`1");
        Assert.Contains(
            applicationBehaviorTypes,
            type => type.FullName == "Bondstone.Modules.ValidationModuleCommandPipelineBehavior`1");
        Assert.DoesNotContain(
            applicationBehaviorTypes,
            type => type.FullName == "Bondstone.Persistence.EntityFrameworkCore.Persistence.EntityFrameworkCoreModuleTransactionBehavior`1");
        Assert.DoesNotContain(
            applicationBehaviorTypes,
            type => type.FullName == "Bondstone.Modules.ModuleExecutionContextPipelineBehavior`1");
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenSystemBehaviorsAreRegistered_RunsThemByOrderBeforeApplicationBehaviors()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CommandCallLog>();
        services.AddScoped<IModuleCommandSystemPipelineBehavior<StoreHandledCommand>, LateSystemBehavior>();
        services.AddScoped<IModuleCommandSystemPipelineBehavior<StoreHandledCommand>, EarlySystemBehavior>();
        services.AddScoped<IModuleCommandPipelineBehavior<StoreHandledCommand>, ApplicationBehavior>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.Commands.RegisterHandler<StoreHandledCommand, LoggingCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "fulfillment",
                new StoreHandledCommand("A-100"));

        CommandCallLog log = serviceProvider.GetRequiredService<CommandCallLog>();

        Assert.Equal(
            [
                "system-early:before",
                "system-late:before",
                "application:before",
                "handle:A-100",
                "application:after",
                "system-late:after",
                "system-early:after",
            ],
            log.Calls);
    }

    public sealed class EarlySystemBehavior(CommandCallLog log)
        : IModuleCommandSystemPipelineBehavior<StoreHandledCommand>
    {
        public int Order => -10;

        public async ValueTask HandleAsync(
            StoreHandledCommand command,
            ModuleCommandExecutionContext context,
            ModuleCommandPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add("system-early:before");
            await next(ct);
            log.Calls.Add("system-early:after");
        }
    }

    public sealed class LateSystemBehavior(CommandCallLog log)
        : IModuleCommandSystemPipelineBehavior<StoreHandledCommand>
    {
        public int Order => 10;

        public async ValueTask HandleAsync(
            StoreHandledCommand command,
            ModuleCommandExecutionContext context,
            ModuleCommandPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add("system-late:before");
            await next(ct);
            log.Calls.Add("system-late:after");
        }
    }

    public sealed class ApplicationBehavior(CommandCallLog log)
        : IModuleCommandPipelineBehavior<StoreHandledCommand>
    {
        public async ValueTask HandleAsync(
            StoreHandledCommand command,
            ModuleCommandExecutionContext context,
            ModuleCommandPipelineNext next,
            CancellationToken ct = default)
        {
            log.Calls.Add("application:before");
            await next(ct);
            log.Calls.Add("application:after");
        }
    }

    public sealed class LoggingCommandHandler(CommandCallLog log)
        : ICommandHandler<StoreHandledCommand>
    {
        public ValueTask HandleAsync(
            StoreHandledCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle:{command.Id}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class CommandCallLog
    {
        public List<string> Calls { get; } = [];
    }

    public sealed record StoreHandledCommand(string Id) : ICommand;

    public sealed class StoreHandledCommandValidator(CommandCallLog log)
        : ICommandValidator<StoreHandledCommand>
    {
        public ValueTask ValidateAsync(
            StoreHandledCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"validate:{command.Id}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class StoreHandledCommandHandler(
        ModuleTransactionTestDbContext context,
        CommandCallLog log)
        : ICommandHandler<StoreHandledCommand>
    {
        public ValueTask HandleAsync(
            StoreHandledCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle:{command.Id}");
            context.HandledCommands.Add(new HandledCommandEntity(command.Id));
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("fulfillment.store-handled.v1")]
    public sealed record StoreDurableHandledCommand(string Id) : IDurableCommand;

    public sealed class StoreDurableHandledCommandHandler(
        FullDurableMappingDbContext context,
        CommandCallLog log)
        : ICommandHandler<StoreDurableHandledCommand>
    {
        public ValueTask HandleAsync(
            StoreDurableHandledCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle:{command.Id}");
            context.HandledCommands.Add(new HandledCommandEntity(command.Id));
            return ValueTask.CompletedTask;
        }
    }

    [IntegrationEventIdentity("fulfillment.store-event.v1")]
    public sealed record StoreDurableHandledEvent(string Id) : IIntegrationEvent;

    public sealed class StoreDurableHandledEventHandler(
        FullDurableMappingDbContext context,
        CommandCallLog log)
        : IIntegrationEventHandler<StoreDurableHandledEvent>
    {
        public ValueTask HandleAsync(
            StoreDurableHandledEvent integrationEvent,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handle-event:{integrationEvent.Id}");
            context.HandledCommands.Add(new HandledCommandEntity($"event:{integrationEvent.Id}"));
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ModuleTransactionTestDbContext(
        DbContextOptions<ModuleTransactionTestDbContext> options,
        CommandCallLog log)
        : DbContext(options)
    {
        public DbSet<HandledCommandEntity> HandledCommands => Set<HandledCommandEntity>();

        public override async Task<int> SaveChangesAsync(
            CancellationToken ct = default)
        {
            log.Calls.Add("save");
            return await base.SaveChangesAsync(ct);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HandledCommandEntity>(
                entity =>
                {
                    entity.HasKey(record => record.Id);
                });
        }
    }

    public sealed class InboxOnlyMappingDbContext(
        DbContextOptions<InboxOnlyMappingDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstoneInbox();
        }
    }

    public sealed class OutboxOnlyMappingDbContext(
        DbContextOptions<OutboxOnlyMappingDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstoneOutbox();
        }
    }

    public sealed class OutboxInboxMappingDbContext(
        DbContextOptions<OutboxInboxMappingDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstoneOutbox();
            modelBuilder.ApplyBondstoneInbox();
        }
    }

    public sealed class FullDurableMappingDbContext(
        DbContextOptions<FullDurableMappingDbContext> options)
        : DbContext(options)
    {
        public DbSet<HandledCommandEntity> HandledCommands => Set<HandledCommandEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyBondstoneOutbox();
            modelBuilder.ApplyBondstoneInbox();
            modelBuilder.ApplyBondstoneOperationState();
            modelBuilder.Entity<HandledCommandEntity>(
                entity =>
                {
                    entity.HasKey(record => record.Id);
                });
        }
    }

    private sealed class CapturingInboxHandlerExecutor(DurableInboxHandleStatus status)
        : IDurableInboxHandlerExecutor
    {
        public async ValueTask<DurableInboxHandleResult> HandleOnceAsync(
            DurableInboxRecord record,
            Func<CancellationToken, ValueTask> handler,
            CancellationToken ct = default)
        {
            if (status == DurableInboxHandleStatus.Handled)
            {
                await handler(ct);
            }

            return new DurableInboxHandleResult(status, record);
        }
    }

    public sealed class HandledCommandEntity(string id)
    {
        public string Id { get; set; } = id;
    }
}
