using Bondstone.Capabilities.DomainEvents;
using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Capabilities.DomainEvents.Tests;

public sealed class DomainEventDispatchTests
{
    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenDispatchIsOptedIn_DispatchesPendingDomainEventsFromFeature()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventDispatchCapture>();
        services.AddScoped<
            IModuleCommandSystemPipelineBehavior<RaiseDomainEventCommand>,
            FakeDomainEventSourceFeatureBehavior>();
        services.AddScoped<
            IDomainEventHandler<InventoryReservedDomainEvent>,
            RecordingInventoryReservedDomainEventHandler>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.UseDomainEventDispatch();
                module.Commands.RegisterHandler<RaiseDomainEventCommand, RaiseDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "fulfillment",
                new RaiseDomainEventCommand("A-100"));

        DomainEventDispatchCapture capture =
            serviceProvider.GetRequiredService<DomainEventDispatchCapture>();
        Assert.Equal(["handler", "domain-handler"], capture.Calls);
        Assert.NotNull(capture.Source);
        Assert.Single(capture.Source.PendingDomainEvents);
        Assert.Equal(0, capture.Source.ClearCount);
    }

    [Fact]
    [Trait("Category", "Application")]
    public async Task ModuleCommands_WhenDispatchIsNotOptedIn_DoesNotDispatchPendingDomainEventsFromFeature()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DomainEventDispatchCapture>();
        services.AddScoped<
            IModuleCommandSystemPipelineBehavior<RaiseDomainEventCommand>,
            FakeDomainEventSourceFeatureBehavior>();
        services.AddScoped<
            IDomainEventHandler<InventoryReservedDomainEvent>,
            RecordingInventoryReservedDomainEventHandler>();

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("fulfillment", module =>
            {
                module.Commands.RegisterHandler<RaiseDomainEventCommand, RaiseDomainEventCommandHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "fulfillment",
                new RaiseDomainEventCommand("A-100"));

        DomainEventDispatchCapture capture =
            serviceProvider.GetRequiredService<DomainEventDispatchCapture>();
        Assert.Equal(["handler"], capture.Calls);
        Assert.NotNull(capture.Source);
        Assert.Single(capture.Source.PendingDomainEvents);
        Assert.Equal(0, capture.Source.ClearCount);
    }

    public sealed record RaiseDomainEventCommand(string InventoryId) : ICommand;

    public sealed class RaiseDomainEventCommandHandler(DomainEventDispatchCapture capture)
        : ICommandHandler<RaiseDomainEventCommand>
    {
        public ValueTask HandleAsync(
            RaiseDomainEventCommand command,
            CancellationToken ct = default)
        {
            capture.Calls.Add("handler");
            capture.Source = DomainEventSource.Reserve(command.InventoryId);
            return ValueTask.CompletedTask;
        }
    }

    public sealed class RecordingInventoryReservedDomainEventHandler(DomainEventDispatchCapture capture)
        : IDomainEventHandler<InventoryReservedDomainEvent>
    {
        public ValueTask HandleAsync(
            InventoryReservedDomainEvent domainEvent,
            CancellationToken ct = default)
        {
            capture.Calls.Add("domain-handler");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class FakeDomainEventSourceFeatureBehavior(DomainEventDispatchCapture capture)
        : IModuleCommandSystemPipelineBehavior<RaiseDomainEventCommand>
    {
        public int Order => ModuleCommandSystemPipelineOrder.ExecutionContext + 10;

        public async ValueTask HandleAsync(
            RaiseDomainEventCommand command,
            ModuleCommandExecutionContext context,
            ModuleCommandPipelineNext next,
            CancellationToken ct = default)
        {
            using IDisposable sourceFeatureScope = context.Features.Push<IDomainEventSourceFeature>(
                new FakeDomainEventSourceFeature(capture));

            await next(ct);
        }
    }

    public sealed class FakeDomainEventSourceFeature(DomainEventDispatchCapture capture)
        : IDomainEventSourceFeature
    {
        public IReadOnlyCollection<IDomainEventSource> GetPendingDomainEventSources()
        {
            return capture.Source is null ? [] : [capture.Source];
        }
    }

    public sealed class DomainEventDispatchCapture
    {
        public List<string> Calls { get; } = [];

        public DomainEventSource? Source { get; set; }
    }

    public sealed class DomainEventSource : IDomainEventSource
    {
        private readonly List<IDomainEvent> _pendingDomainEvents = [];

        public IReadOnlyCollection<IDomainEvent> PendingDomainEvents => _pendingDomainEvents;

        public int ClearCount { get; private set; }

        public static DomainEventSource Reserve(string inventoryId)
        {
            var source = new DomainEventSource();
            source._pendingDomainEvents.Add(new InventoryReservedDomainEvent(inventoryId));
            return source;
        }

        public void ClearPendingDomainEvents()
        {
            ClearCount++;
            _pendingDomainEvents.Clear();
        }
    }

    [DomainEventIdentity("inventory.reserved.v1")]
    public sealed record InventoryReservedDomainEvent(string InventoryId) : IDomainEvent;
}
