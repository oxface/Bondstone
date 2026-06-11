using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Bondstone.Persistence.Postgres.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace Bondstone.Persistence.Postgres.Tests;

public sealed class PostgresModuleRuntimeTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommand_WhenModuleUsesPostgresPersistence_RunsInsidePostgresTransaction()
    {
        var log = new CommandCallLog();
        var session = new RecordingPostgresModuleSession(log);
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddScoped<IPostgresModuleSession>(_ => session);
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("billing", module =>
            {
                module.UsePostgresPersistence(
                    "Host=localhost;Database=bondstone_tests");
                module.Commands.RegisterHandler<StoreCommand, StoreCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "billing",
                new StoreCommand("A-100"));

        Assert.Equal(
            ["postgres:begin", "handler:A-100", "postgres:commit"],
            log.Calls);
        Assert.Equal(1, session.TransactionCalls);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommand_WhenModuleDeclaresEntityFrameworkCoreProviderWithPostgresInHost_DoesNotResolvePostgresSession()
    {
        var log = new CommandCallLog();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        RegisterThrowingPostgresModuleSession(services);
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("billing", module =>
            {
                module.UsePostgresPersistence(
                    "Host=localhost;Database=bondstone_tests");
            });
            bondstone.Module("fulfillment", module =>
            {
                module.UsePersistence("EntityFrameworkCore");
                module.Commands.RegisterHandler<StoreCommand, StoreCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "fulfillment",
                new StoreCommand("A-100"));

        Assert.Equal(["handler:A-100"], log.Calls);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommand_WhenOtherPersistenceModuleSharesHostWithPostgres_DoesNotResolvePostgresSession()
    {
        var log = new CommandCallLog();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        RegisterThrowingPostgresModuleSession(services);
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("billing", module =>
            {
                module.UsePostgresPersistence(
                    "Host=localhost;Database=bondstone_tests");
            });
            bondstone.Module("fulfillment", module =>
            {
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<StoreCommand, StoreCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "fulfillment",
                new StoreCommand("A-100"));

        Assert.Equal(["handler:A-100"], log.Calls);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommand_WhenModuleDoesNotDeclarePersistence_DoesNotResolvePostgresSession()
    {
        var log = new CommandCallLog();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        RegisterThrowingPostgresModuleSession(services);
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("billing", module =>
            {
                module.UsePostgresPersistence(
                    "Host=localhost;Database=bondstone_tests");
            });
            bondstone.Module("fulfillment", module =>
            {
                module.Commands.RegisterHandler<StoreCommand, StoreCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "fulfillment",
                new StoreCommand("A-100"));

        Assert.Equal(["handler:A-100"], log.Calls);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task DurableSend_WhenAnotherPostgresModuleSharesHost_DoesNotResolvePostgresSession()
    {
        var log = new CommandCallLog();
        var writer = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddSingleton(new DurableModuleOutboxWriterRegistration(
            "fulfillment",
            _ => writer));
        RegisterThrowingPostgresModuleSession(services);
        services.AddBondstone(bondstone =>
        {
            bondstone.Module("billing", module =>
            {
                module.UsePostgresPersistence(
                    "Host=localhost;Database=bondstone_tests");
            });
            bondstone.Module("fulfillment", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<SendStoreCommand, SendStoreCommandHandler>();
                module.Commands.RegisterHandler<StoreDurableCommand, StoreDurableCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "fulfillment",
                new SendStoreCommand("A-100"));

        Assert.Equal(["send:A-100"], log.Calls);
        DurableMessageEnvelope envelope = Assert.Single(writer.Envelopes);
        Assert.Equal("fulfillment", envelope.SourceModule);
        Assert.Equal("fulfillment", envelope.TargetModule);
    }

    public sealed record StoreCommand(string Id) : ICommand;

    public sealed record SendStoreCommand(string Id) : ICommand;

    [DurableCommandIdentity("fulfillment.store.v1")]
    public sealed record StoreDurableCommand(string Id) : IDurableCommand;

    public sealed class StoreCommandHandler(CommandCallLog log)
        : ICommandHandler<StoreCommand>
    {
        public ValueTask HandleAsync(
            StoreCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"handler:{command.Id}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class SendStoreCommandHandler(
        IDurableCommandSender sender,
        CommandCallLog log)
        : ICommandHandler<SendStoreCommand>
    {
        public async ValueTask HandleAsync(
            SendStoreCommand command,
            CancellationToken ct = default)
        {
            log.Calls.Add($"send:{command.Id}");
            await sender.SendAsync(
                new StoreDurableCommand(command.Id),
                "fulfillment",
                ct);
        }
    }

    public sealed class StoreDurableCommandHandler : ICommandHandler<StoreDurableCommand>
    {
        public ValueTask HandleAsync(
            StoreDurableCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    public sealed class CommandCallLog
    {
        public List<string> Calls { get; } = [];
    }

    private sealed class CapturingOutboxWriter : IDurableOutboxWriter
    {
        public List<DurableMessageEnvelope> Envelopes { get; } = [];

        public ValueTask WriteAsync(
            DurableMessageEnvelope envelope,
            CancellationToken ct = default)
        {
            Envelopes.Add(envelope);
            return ValueTask.CompletedTask;
        }
    }

    private static void RegisterThrowingPostgresModuleSession(IServiceCollection services)
    {
        services.AddScoped<IPostgresModuleSession>(_ =>
            throw new InvalidOperationException("Postgres session should not be resolved."));
    }

    private sealed class RecordingPostgresModuleSession(CommandCallLog log)
        : IPostgresModuleSession
    {
        public NpgsqlConnection Connection =>
            throw new NotSupportedException("The fake session does not expose a connection.");

        public NpgsqlTransaction? Transaction => null;

        public int TransactionCalls { get; private set; }

        public ValueTask EnsureOpenAsync(CancellationToken ct = default)
        {
            throw new NotSupportedException("The fake session does not open a connection.");
        }

        public async ValueTask ExecuteInTransactionAsync(
            Func<CancellationToken, ValueTask> operation,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            TransactionCalls++;
            log.Calls.Add("postgres:begin");
            await operation(ct);
            log.Calls.Add("postgres:commit");
        }
    }
}
