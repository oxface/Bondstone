using System.Reflection;
using Bondstone.Diagnostics;
using Bondstone.Utility;

namespace Bondstone.Messaging;

public sealed class MessageTypeRegistry : IMessageTypeRegistry
{
    private readonly Dictionary<Type, MessageTypeRegistration> _registrationsByClrType = [];
    private readonly Dictionary<string, MessageTypeRegistration> _registrationsByMessageTypeName =
        new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public MessageTypeRegistration Register<TMessage>()
        where TMessage : IMessage
    {
        return Register(typeof(TMessage));
    }

    public MessageTypeRegistration Register(Type clrType)
    {
        ArgumentNullException.ThrowIfNull(clrType);

        MessageTypeRegistration registration = MessageIdentityMetadata.GetRequiredRegistration(clrType);
        return Register(registration.ClrType, registration.MessageTypeName, registration.Kind);
    }

    public MessageTypeRegistration Register<TMessage>(string messageTypeName)
        where TMessage : IMessage
    {
        return Register(typeof(TMessage), messageTypeName);
    }

    public MessageTypeRegistration Register(Type clrType, string messageTypeName)
    {
        ArgumentNullException.ThrowIfNull(clrType);

        MessageKind kind = GetMessageKind(clrType);
        return Register(clrType, messageTypeName, kind);
    }

    public IReadOnlyCollection<MessageTypeRegistration> RegisterFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false }
                && typeof(IMessage).IsAssignableFrom(type)
                && MessageIdentityMetadata.HasIdentity(type))
            .Select(Register)
            .ToArray();
    }

    public string GetMessageTypeName<TMessage>()
        where TMessage : IMessage
    {
        return GetMessageTypeName(typeof(TMessage));
    }

    public string GetMessageTypeName(Type clrType)
    {
        ArgumentNullException.ThrowIfNull(clrType);

        lock (_sync)
        {
            if (_registrationsByClrType.TryGetValue(clrType, out MessageTypeRegistration? registration))
            {
                return registration.MessageTypeName;
            }
        }

        throw new KeyNotFoundException($"No message type registration exists for '{clrType.FullName}'.");
    }

    public Type ResolveClrType(string messageTypeName)
    {
        if (TryResolveClrType(messageTypeName, out Type? clrType))
        {
            return clrType!;
        }

        throw new KeyNotFoundException($"No message CLR type registration exists for '{messageTypeName}'.");
    }

    public bool TryResolveClrType(string messageTypeName, out Type? clrType)
    {
        string? normalizedTypeName = messageTypeName.NormalizeOptional();
        if (normalizedTypeName is null)
        {
            clrType = null;
            return false;
        }

        lock (_sync)
        {
            if (_registrationsByMessageTypeName.TryGetValue(
                normalizedTypeName,
                out MessageTypeRegistration? registration))
            {
                clrType = registration.ClrType;
                return true;
            }
        }

        clrType = null;
        return false;
    }

    public IReadOnlyCollection<MessageTypeRegistration> Registrations
    {
        get
        {
            lock (_sync)
            {
                return _registrationsByClrType.Values.ToArray();
            }
        }
    }

    private MessageTypeRegistration Register(Type clrType, string messageTypeName, MessageKind kind)
    {
        if (!typeof(IMessage).IsAssignableFrom(clrType))
        {
            throw new BondstoneSetupArgumentException(
                BondstoneSetupCodes.InvalidDurableIdentity,
                $"Type '{clrType.FullName}' must implement {nameof(IMessage)}.",
                nameof(clrType));
        }

        string normalizedTypeName = NormalizeMessageTypeName(messageTypeName);
        var registration = new MessageTypeRegistration(clrType, normalizedTypeName, kind);

        lock (_sync)
        {
            if (_registrationsByClrType.TryGetValue(clrType, out MessageTypeRegistration? existingRegistration))
            {
                if (existingRegistration != registration)
                {
                    throw new BondstoneSetupException(
                        BondstoneSetupCodes.DuplicateDurableRegistration,
                        $"Type '{clrType.FullName}' is already registered as '{existingRegistration.MessageTypeName}'.");
                }

                return existingRegistration;
            }

            if (_registrationsByMessageTypeName.TryGetValue(
                normalizedTypeName,
                out MessageTypeRegistration? existingTypeNameRegistration))
            {
                throw new BondstoneSetupException(
                    BondstoneSetupCodes.DuplicateDurableRegistration,
                    $"Message type '{normalizedTypeName}' is already registered for '{existingTypeNameRegistration.ClrType.FullName}'.");
            }

            _registrationsByClrType.Add(clrType, registration);
            _registrationsByMessageTypeName.Add(normalizedTypeName, registration);
            return registration;
        }
    }

    private static string NormalizeMessageTypeName(string? messageTypeName)
    {
        return string.IsNullOrWhiteSpace(messageTypeName)
            ? throw new BondstoneSetupArgumentException(
                BondstoneSetupCodes.InvalidDurableIdentity,
                "Message type name is required.",
                nameof(messageTypeName))
            : messageTypeName.Trim();
    }

    private static MessageKind GetMessageKind(Type clrType)
    {
        bool isDurableCommand = typeof(IDurableCommand).IsAssignableFrom(clrType);
        bool isIntegrationEvent = typeof(IIntegrationEvent).IsAssignableFrom(clrType);

        return (isDurableCommand, isIntegrationEvent) switch
        {
            (true, false) => MessageKind.Command,
            (false, true) => MessageKind.Event,
            (true, true) => throw new BondstoneSetupArgumentException(
                BondstoneSetupCodes.InvalidDurableIdentity,
                $"Message type '{clrType.FullName}' must not implement both {nameof(IDurableCommand)} and {nameof(IIntegrationEvent)}.",
                nameof(clrType)),
            _ => throw new BondstoneSetupArgumentException(
                BondstoneSetupCodes.InvalidDurableIdentity,
                $"Type '{clrType.FullName}' must implement {nameof(IDurableCommand)} or {nameof(IIntegrationEvent)}.",
                nameof(clrType)),
        };
    }

}
