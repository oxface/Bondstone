using Bondstone.Configuration;
using Bondstone.Messaging;
using Bondstone.Modules;
using Bondstone.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Bondstone.Tests.Messaging;

public sealed class DurableCommandSenderTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenCalledInsideModuleCommand_StagesEnvelopeWithCurrentModuleAsSource()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);
        services.AddSingleton<TimeProvider>(
            new FixedTimeProvider(DateTimeOffset.Parse("2026-06-06T12:00:00+00:00")));

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<SubmitOrderCommand, SubmitOrderHandler>();
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
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
        Assert.Equal(MessageKind.Command, envelope.MessageKind);
        Assert.Equal("sales.order.reserve.v1", envelope.MessageTypeName);
        Assert.Equal("sales", envelope.SourceModule);
        Assert.Equal("fulfillment", envelope.TargetModule);
        Assert.Equal("""{"orderId":"order-123"}""", envelope.Payload);
        Assert.Equal("order-123", envelope.PartitionKey);
        Assert.Equal(DateTimeOffset.Parse("2026-06-06T12:00:00+00:00"), envelope.CreatedAtUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_UsesConfiguredDurablePayloadJsonOptions()
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
                module.Commands.RegisterHandler<
                    SubmitConvertedOrderCommand,
                    SubmitConvertedOrderHandler>();
                module.Commands.RegisterHandler<
                    ReserveConvertedOrderCommand,
                    ReserveConvertedOrderHandler>();
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
        Assert.Equal("sales.order.reserve-converted.v1", envelope.MessageTypeName);
        Assert.Equal("""{"orderId":"payload-order-123"}""", envelope.Payload);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SendAsync_WhenNoModuleExecutionContextExists_Throws()
    {
        var outboxWriter = new CapturingOutboxWriter();
        var services = new ServiceCollection();
        services.AddSingleton<IDurableOutboxWriter>(outboxWriter);

        services.AddBondstone(bondstone =>
        {
            bondstone.Module("sales", module =>
            {
                module.UseDurableMessaging();
                module.UsePersistence("test persistence");
                module.Commands.RegisterHandler<ReserveOrderCommand, ReserveOrderHandler>();
            });
        });

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using IServiceScope scope = serviceProvider.CreateScope();
        IDurableCommandSender sender =
            scope.ServiceProvider.GetRequiredService<IDurableCommandSender>();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await sender.SendAsync(
                new ReserveOrderCommand("order-123"),
                "fulfillment"));

        Assert.Contains("module execution context", exception.Message, StringComparison.Ordinal);
        Assert.Empty(outboxWriter.Envelopes);
    }

    [DurableCommandIdentity("sales.order.submit.v1")]
    public sealed record SubmitOrderCommand(string OrderId) : IDurableCommand;

    public sealed class SubmitOrderHandler(IDurableCommandSender sender)
        : ICommandHandler<SubmitOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitOrderCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new ReserveOrderCommand(command.OrderId),
                "fulfillment",
                partitionKey: command.OrderId,
                ct: ct);
        }
    }

    [DurableCommandIdentity("sales.order.reserve.v1")]
    public sealed record ReserveOrderCommand(string OrderId) : IDurableCommand;

    public sealed class ReserveOrderHandler : ICommandHandler<ReserveOrderCommand>
    {
        public ValueTask HandleAsync(
            ReserveOrderCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
    }

    [DurableCommandIdentity("sales.order.submit-converted.v1")]
    public sealed record SubmitConvertedOrderCommand(string OrderId) : IDurableCommand;

    public sealed class SubmitConvertedOrderHandler(IDurableCommandSender sender)
        : ICommandHandler<SubmitConvertedOrderCommand>
    {
        public async ValueTask HandleAsync(
            SubmitConvertedOrderCommand command,
            CancellationToken ct = default)
        {
            await sender.SendAsync(
                new ReserveConvertedOrderCommand(new DurableOrderId(command.OrderId)),
                "fulfillment",
                ct);
        }
    }

    [DurableCommandIdentity("sales.order.reserve-converted.v1")]
    public sealed record ReserveConvertedOrderCommand(DurableOrderId OrderId)
        : IDurableCommand;

    public sealed class ReserveConvertedOrderHandler
        : ICommandHandler<ReserveConvertedOrderCommand>
    {
        public ValueTask HandleAsync(
            ReserveConvertedOrderCommand command,
            CancellationToken ct = default)
        {
            return ValueTask.CompletedTask;
        }
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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
