using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableEventPublisherTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task PublishAsync_WhenCalledInsideModuleCommand_StagesEventEnvelopeWithCurrentModuleAsSource()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);
        services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-07T12:00:00+00:00")));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Events.RegisterPublishedEvent<OrderSubmittedEvent>();
                module.Commands.RegisterHandler<SubmitOrderCommand, SubmitOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SubmitOrderCommand("order-123"));

        DurableMessageEnvelope envelope = Assert.Single(outboxWriter.Envelopes);
        Assert.Equal(MessageKind.Event, envelope.MessageKind);
        Assert.Equal("sales.order.submitted.v1", envelope.MessageTypeName);
        Assert.Equal("sales", envelope.SourceModule);
        Assert.Null(envelope.TargetModule);
        Assert.Equal("""{"orderId":"order-123"}""", envelope.Payload);
        Assert.Equal("order-123", envelope.PartitionKey);
        Assert.Equal(DateTimeOffset.Parse("2026-06-07T12:00:00+00:00"), envelope.CreatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PublishAsync_UsesConfiguredDurablePayloadJsonOptions()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);
        services.ConfigureBondstoneDurablePayloadJson(
            options => options.Converters.Add(new DurableOrderIdJsonConverter()));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Events.RegisterPublishedEvent<ConvertedOrderSubmittedEvent>();
                module.Commands.RegisterHandler<
                    SubmitConvertedOrderCommand,
                    SubmitConvertedOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();

        await scope.ServiceProvider
            .GetRequiredService<IModuleCommandExecutor>()
            .ExecuteAsync(
                "sales",
                new SubmitConvertedOrderCommand("order-123"));

        DurableMessageEnvelope envelope = Assert.Single(outboxWriter.Envelopes);
        Assert.Equal(MessageKind.Event, envelope.MessageKind);
        Assert.Equal("sales.order.submitted-converted.v1", envelope.MessageTypeName);
        Assert.Equal("""{"orderId":"payload-order-123"}""", envelope.Payload);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PublishAsync_WhenNoModuleExecutionContextExists_Throws()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.Events.RegisterPublishedEvent<OrderSubmittedEvent>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableEventPublisher publisher =
            scope.ServiceProvider.GetRequiredService<IDurableEventPublisher>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await publisher.PublishAsync(
                new OrderSubmittedEvent("order-123")));

        Assert.Contains("module execution context", exception.Message, StringComparison.Ordinal);
        Assert.Empty(outboxWriter.Envelopes);
    }

    [DurableCommandIdentity("sales.order.publish-test.submit.v1")]
    public sealed record SubmitOrderCommand(string OrderId) : IDurableCommand;

    public sealed class SubmitOrderHandler(IDurableEventPublisher publisher)
        : ICommandHandler<SubmitOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitOrderCommand command,
            CancellationToken ct = default)
        {
            await publisher.PublishAsync(
                new OrderSubmittedEvent(command.OrderId),
                partitionKey: command.OrderId,
                ct: ct);
        }
    }

    [IntegrationEventIdentity("sales.order.submitted.v1")]
    public sealed record OrderSubmittedEvent(string OrderId) : IIntegrationEvent;

    [DurableCommandIdentity("sales.order.publish-test.submit-converted.v1")]
    public sealed record SubmitConvertedOrderCommand(string OrderId) : IDurableCommand;

    public sealed class SubmitConvertedOrderHandler(IDurableEventPublisher publisher)
        : ICommandHandler<SubmitConvertedOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitConvertedOrderCommand command,
            CancellationToken ct = default)
        {
            await publisher.PublishAsync(
                new ConvertedOrderSubmittedEvent(new DurableOrderId(command.OrderId)),
                ct: ct);
        }
    }

    [IntegrationEventIdentity("sales.order.submitted-converted.v1")]
    public sealed record ConvertedOrderSubmittedEvent(DurableOrderId OrderId)
        : IIntegrationEvent;

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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
