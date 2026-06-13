using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class MessageTypeRegistryTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WithExplicitDurableCommandName_ResolvesByClrTypeAndMessageTypeName()
    {
        var registry = new MessageTypeRegistry();

        MessageTypeRegistration registration = registry.Register<RegisterCustomerCommand>("sales.customer.register.v1");

        Assert.Equal(typeof(RegisterCustomerCommand), registration.ClrType);
        Assert.Equal("sales.customer.register.v1", registration.MessageTypeName);
        Assert.Equal(MessageKind.Command, registration.Kind);
        Assert.Equal("sales.customer.register.v1", registry.GetMessageTypeName<RegisterCustomerCommand>());
        Assert.Equal(typeof(RegisterCustomerCommand), registry.ResolveClrType("sales.customer.register.v1"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WithExplicitIntegrationEventName_ResolvesEventKind()
    {
        var registry = new MessageTypeRegistry();

        MessageTypeRegistration registration = registry.Register<CustomerRegistered>("sales.customer.registered.v1");

        Assert.Equal(typeof(CustomerRegistered), registration.ClrType);
        Assert.Equal("sales.customer.registered.v1", registration.MessageTypeName);
        Assert.Equal(MessageKind.Event, registration.Kind);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WithExplicitName_TrimsBoundaryWhitespace()
    {
        var registry = new MessageTypeRegistry();

        MessageTypeRegistration registration = registry.Register<RegisterCustomerCommand>("  sales.customer.register.v1  ");

        Assert.Equal("sales.customer.register.v1", registration.MessageTypeName);
        Assert.Equal(typeof(RegisterCustomerCommand), registry.ResolveClrType(" sales.customer.register.v1 "));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WhenSameClrTypeAndNameIsRegisteredTwice_ReturnsExistingRegistration()
    {
        var registry = new MessageTypeRegistry();

        MessageTypeRegistration first = registry.Register<RegisterCustomerCommand>("sales.customer.register.v1");
        MessageTypeRegistration second = registry.Register<RegisterCustomerCommand>("sales.customer.register.v1");

        Assert.Same(first, second);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WhenClrTypeIsRegisteredWithDifferentName_Throws()
    {
        var registry = new MessageTypeRegistry();
        registry.Register<RegisterCustomerCommand>("sales.customer.register.v1");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => registry.Register<RegisterCustomerCommand>("sales.customer.register.v2"));

        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WhenMessageTypeNameIsRegisteredForDifferentClrType_Throws()
    {
        var registry = new MessageTypeRegistry();
        registry.Register<RegisterCustomerCommand>("sales.customer.message.v1");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => registry.Register<CustomerRegistered>("sales.customer.message.v1"));

        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WithIdentityAttribute_UsesStableMessageIdentity()
    {
        var registry = new MessageTypeRegistry();

        MessageTypeRegistration registration = registry.Register<AttributedCommand>();

        Assert.Equal("sales.customer.attributed.command.v1", registration.MessageTypeName);
        Assert.Equal(MessageKind.Command, registration.Kind);
        Assert.Equal(typeof(AttributedCommand), registry.ResolveClrType("sales.customer.attributed.command.v1"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WithIdentityAttribute_TrimsBoundaryWhitespace()
    {
        var registry = new MessageTypeRegistry();

        MessageTypeRegistration registration = registry.Register<AttributedCommandWithWhitespace>();

        Assert.Equal("sales.customer.attributed.whitespace.v1", registration.MessageTypeName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WhenDurableCommandUsesIntegrationEventIdentity_Throws()
    {
        var registry = new MessageTypeRegistry();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            registry.Register<CommandWithEventIdentity>);

        Assert.Contains(nameof(DurableCommandIdentityAttribute), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(IntegrationEventIdentityAttribute), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WhenIntegrationEventUsesDurableCommandIdentity_Throws()
    {
        var registry = new MessageTypeRegistry();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            registry.Register<EventWithCommandIdentity>);

        Assert.Contains(nameof(IntegrationEventIdentityAttribute), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DurableCommandIdentityAttribute), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void RegisterFromAssembly_RegistersOnlyMessagesWithExplicitIdentities()
    {
        var registry = new MessageTypeRegistry();

        IReadOnlyCollection<MessageTypeRegistration> registrations =
            registry.RegisterFromAssembly(typeof(AttributedCommand).Assembly);

        Assert.Contains(
            registrations,
            registration => registration.ClrType == typeof(AttributedCommand)
                && registration.MessageTypeName == "sales.customer.attributed.command.v1"
                && registration.Kind == MessageKind.Command);
        Assert.Contains(
            registrations,
            registration => registration.ClrType == typeof(AttributedEvent)
                && registration.MessageTypeName == "sales.customer.attributed.event.v1"
                && registration.Kind == MessageKind.Event);
        Assert.DoesNotContain(registrations, registration => registration.ClrType == typeof(RegisterCustomerCommand));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TryResolveClrType_WhenNameIsUnknown_ReturnsFalse()
    {
        var registry = new MessageTypeRegistry();

        bool found = registry.TryResolveClrType("sales.customer.unknown.v1", out Type? clrType);

        Assert.False(found);
        Assert.Null(clrType);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WhenTypeImplementsBothCommandAndEvent_Throws()
    {
        var registry = new MessageTypeRegistry();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => registry.Register<AmbiguousMessage>("sales.customer.ambiguous.v1"));

        Assert.Contains(nameof(IDurableCommand), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(IIntegrationEvent), exception.Message, StringComparison.Ordinal);
    }

    private sealed record RegisterCustomerCommand : IDurableCommand;

    private sealed record CustomerRegistered : IIntegrationEvent;

    [DurableCommandIdentity("sales.customer.attributed.command.v1")]
    private sealed record AttributedCommand : IDurableCommand;

    [DurableCommandIdentity("  sales.customer.attributed.whitespace.v1  ")]
    private sealed record AttributedCommandWithWhitespace : IDurableCommand;

    [IntegrationEventIdentity("sales.customer.attributed.event.v1")]
    private sealed record AttributedEvent : IIntegrationEvent;

    [IntegrationEventIdentity("sales.customer.wrong.command.v1")]
    private sealed record CommandWithEventIdentity : IDurableCommand;

    [DurableCommandIdentity("sales.customer.wrong.event.v1")]
    private sealed record EventWithCommandIdentity : IIntegrationEvent;

    private sealed record AmbiguousMessage : IDurableCommand, IIntegrationEvent;
}
