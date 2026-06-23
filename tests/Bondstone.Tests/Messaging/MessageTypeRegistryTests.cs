using System.Reflection;
using System.Reflection.Emit;
using Bondstone.Diagnostics;
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

        InvalidOperationException exception = Assert.ThrowsAny<InvalidOperationException>(
            () => registry.Register<RegisterCustomerCommand>("sales.customer.register.v2"));

        Assert.Equal(
            BondstoneSetupCodes.DuplicateDurableRegistration,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
        Assert.Contains("already registered", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WhenMessageTypeNameIsRegisteredForDifferentClrType_Throws()
    {
        var registry = new MessageTypeRegistry();
        registry.Register<RegisterCustomerCommand>("sales.customer.message.v1");

        InvalidOperationException exception = Assert.ThrowsAny<InvalidOperationException>(
            () => registry.Register<CustomerRegistered>("sales.customer.message.v1"));

        Assert.Equal(
            BondstoneSetupCodes.DuplicateDurableRegistration,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
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
    public void Register_WithBlankExplicitName_ThrowsSetupCode()
    {
        var registry = new MessageTypeRegistry();

        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(
            () => registry.Register<RegisterCustomerCommand>(" "));

        Assert.Equal(
            BondstoneSetupCodes.InvalidDurableIdentity,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
        Assert.Equal("messageTypeName", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WithBlankIdentityAttribute_ThrowsSetupCode()
    {
        var registry = new MessageTypeRegistry();
        Type messageType = CreateAttributedCommandTypeWithBlankIdentity();

        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(
            () => registry.Register(messageType));

        Assert.Equal(
            BondstoneSetupCodes.InvalidDurableIdentity,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
        Assert.Equal(nameof(DurableCommandIdentityAttribute.Name), exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WhenDurableCommandUsesIntegrationEventIdentity_Throws()
    {
        var registry = new MessageTypeRegistry();

        InvalidOperationException exception = Assert.ThrowsAny<InvalidOperationException>(
            registry.Register<CommandWithEventIdentity>);

        Assert.Equal(
            BondstoneSetupCodes.InvalidDurableIdentity,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
        Assert.Contains(nameof(DurableCommandIdentityAttribute), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(IntegrationEventIdentityAttribute), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Register_WhenIntegrationEventUsesDurableCommandIdentity_Throws()
    {
        var registry = new MessageTypeRegistry();

        InvalidOperationException exception = Assert.ThrowsAny<InvalidOperationException>(
            registry.Register<EventWithCommandIdentity>);

        Assert.Equal(
            BondstoneSetupCodes.InvalidDurableIdentity,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
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

        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(
            () => registry.Register<AmbiguousMessage>("sales.customer.ambiguous.v1"));

        Assert.Equal(
            BondstoneSetupCodes.InvalidDurableIdentity,
            Assert.IsAssignableFrom<IBondstoneSetupException>(exception).SetupCode);
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

    private static Type CreateAttributedCommandTypeWithBlankIdentity()
    {
        AssemblyName assemblyName = new("Bondstone.Tests.DynamicMessages");
        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(
            assemblyName.Name!);
        TypeBuilder typeBuilder = moduleBuilder.DefineType(
            "BlankIdentityCommand",
            TypeAttributes.NotPublic | TypeAttributes.Class);
        typeBuilder.AddInterfaceImplementation(typeof(IDurableCommand));

        ConstructorInfo constructor =
            typeof(DurableCommandIdentityAttribute).GetConstructor([typeof(string)])!;
        typeBuilder.SetCustomAttribute(
            new CustomAttributeBuilder(constructor, [" "]));

        return typeBuilder.CreateType();
    }
}
