using Bondstone.DomainEvents;
using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.DomainEvents;

public sealed class DomainEventContractTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void DomainEvent_IsModuleLocalDomainMarker()
    {
        object domainEvent = new InventoryReservedDomainEvent("inventory-1");

        Assert.IsAssignableFrom<IDomainEvent>(domainEvent);
        Assert.False(domainEvent is IMessage);
        Assert.False(domainEvent is IDurableCommand);
        Assert.False(domainEvent is IIntegrationEvent);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DomainEventIdentityAttribute_ExposesStableModuleLocalIdentity()
    {
        var attribute = Assert.Single(
            typeof(InventoryReservedDomainEvent)
                .GetCustomAttributes(typeof(DomainEventIdentityAttribute), inherit: false)
                .OfType<DomainEventIdentityAttribute>());

        Assert.Equal("inventory.reserved.v1", attribute.Name);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DomainEventSource_ExposesPendingEventsAndClearsThemExplicitly()
    {
        var source = new InventoryReservation("inventory-1");
        source.Reserve();

        IDomainEvent domainEvent = Assert.Single(source.PendingDomainEvents);
        Assert.IsType<InventoryReservedDomainEvent>(domainEvent);

        source.ClearPendingDomainEvents();

        Assert.Empty(source.PendingDomainEvents);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MessageTypeRegistry_WhenDomainEventHasIdentity_DoesNotTreatItAsDurableMessage()
    {
        var registry = new MessageTypeRegistry();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => registry.Register(typeof(InventoryReservedDomainEvent), "inventory.reserved.v1"));

        Assert.Equal("clrType", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DomainEventHandler_WhenImplemented_HandlesLocalDomainEvent()
    {
        var handler = new InventoryReservedHandler();
        var domainEvent = new InventoryReservedDomainEvent("inventory-1");

        await handler.HandleAsync(domainEvent);

        Assert.Equal(domainEvent, handler.HandledDomainEvent);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void DomainEventHandler_GenericArgumentIsConstrainedToDomainEvent()
    {
        Type genericParameter = typeof(IDomainEventHandler<>).GetGenericArguments()[0];

        Assert.Contains(
            typeof(IDomainEvent),
            genericParameter.GetGenericParameterConstraints());
    }

    private sealed class InventoryReservation(string inventoryId) : IDomainEventSource
    {
        private readonly List<IDomainEvent> _pendingDomainEvents = [];

        public IReadOnlyCollection<IDomainEvent> PendingDomainEvents => _pendingDomainEvents;

        public void Reserve()
        {
            _pendingDomainEvents.Add(new InventoryReservedDomainEvent(inventoryId));
        }

        public void ClearPendingDomainEvents()
        {
            _pendingDomainEvents.Clear();
        }
    }

    private sealed class InventoryReservedHandler : IDomainEventHandler<InventoryReservedDomainEvent>
    {
        public InventoryReservedDomainEvent? HandledDomainEvent { get; private set; }

        public ValueTask HandleAsync(
            InventoryReservedDomainEvent domainEvent,
            CancellationToken ct = default)
        {
            HandledDomainEvent = domainEvent;
            return ValueTask.CompletedTask;
        }
    }

    [DomainEventIdentity("inventory.reserved.v1")]
    private sealed record InventoryReservedDomainEvent(string InventoryId) : IDomainEvent;
}
