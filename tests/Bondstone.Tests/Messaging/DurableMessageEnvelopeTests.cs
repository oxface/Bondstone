using Bondstone.Messaging;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableMessageEnvelopeTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenCommandValuesAreValid_CarriesEnvelopeFields()
    {
        Guid messageId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        Guid durableOperationId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        Guid causationId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var traceContext = new MessageTraceContext("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-00");
        DateTimeOffset createdAtUtc = DateTimeOffset.Parse("2026-06-04T00:00:00+00:00");

        var envelope = new DurableMessageEnvelope(
            messageId,
            MessageKind.Command,
            " sales.customer.register.v1 ",
            " sales ",
            " billing ",
            """ {"customerId":"customer-123"} """,
            createdAtUtc,
            durableOperationId,
            traceContext,
            causationId,
            " customer-123 ",
            """ {"metadata":true} """);

        Assert.Equal(messageId, envelope.MessageId);
        Assert.Equal(MessageKind.Command, envelope.MessageKind);
        Assert.Equal("sales.customer.register.v1", envelope.MessageTypeName);
        Assert.Equal("sales", envelope.SourceModule);
        Assert.Equal("billing", envelope.TargetModule);
        Assert.Equal(durableOperationId, envelope.DurableOperationId);
        Assert.Same(traceContext, envelope.TraceContext);
        Assert.Equal(causationId, envelope.CausationId);
        Assert.Equal("customer-123", envelope.PartitionKey);
        Assert.Equal(""" {"customerId":"customer-123"} """, envelope.Payload);
        Assert.Equal(""" {"metadata":true} """, envelope.Metadata);
        Assert.Equal(createdAtUtc, envelope.CreatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenEventValuesAreValid_AllowsNoTargetModule()
    {
        var envelope = new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Event,
            "sales.customer.registered.v1",
            "sales",
            targetModule: null,
            payload: "{}",
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));

        Assert.Null(envelope.TargetModule);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenMessageIdIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateCommandEnvelope(messageId: Guid.Empty));

        Assert.Equal("messageId", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenMessageKindIsUnknown_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateCommandEnvelope(messageKind: (MessageKind)999));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenMessageTypeNameIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateCommandEnvelope(messageTypeName: " "));

        Assert.Equal("messageTypeName", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenCommandTargetModuleIsMissing_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateCommandEnvelope(targetModule: " "));

        Assert.Equal("targetModule", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenEventTargetModuleIsSpecified_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateEventEnvelope(targetModule: "billing"));

        Assert.Equal("targetModule", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenPayloadIsEmpty_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateCommandEnvelope(payload: " "));

        Assert.Equal("payload", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenCreatedAtUtcIsDefault_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateCommandEnvelope(createdAtUtc: DateTimeOffset.MinValue));

        Assert.Equal("createdAtUtc", exception.ParamName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Constructor_WhenCreatedAtUtcHasNonUtcOffset_Throws()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => CreateCommandEnvelope(createdAtUtc: DateTimeOffset.Parse("2026-06-04T00:00:00+02:00")));

        Assert.Equal("createdAtUtc", exception.ParamName);
    }

    private static DurableMessageEnvelope CreateCommandEnvelope(
        Guid? messageId = null,
        MessageKind messageKind = MessageKind.Command,
        string messageTypeName = "sales.customer.register.v1",
        string sourceModule = "sales",
        string? targetModule = "billing",
        string payload = "{}",
        DateTimeOffset? createdAtUtc = null)
    {
        return new DurableMessageEnvelope(
            messageId ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            messageKind,
            messageTypeName,
            sourceModule,
            targetModule,
            payload,
            createdAtUtc ?? DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
    }

    private static DurableMessageEnvelope CreateEventEnvelope(string? targetModule = null)
    {
        return new DurableMessageEnvelope(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            MessageKind.Event,
            "sales.customer.registered.v1",
            "sales",
            targetModule,
            "{}",
            DateTimeOffset.Parse("2026-06-04T00:00:00+00:00"));
    }
}
