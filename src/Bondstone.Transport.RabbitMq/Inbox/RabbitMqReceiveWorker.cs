using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Bondstone.Transport.RabbitMq.Inbox;

internal sealed class RabbitMqReceiveWorker(
    IConnection connection,
    IServiceScopeFactory serviceScopeFactory,
    RabbitMqReceiveTopology receiveTopology,
    IOptions<RabbitMqReceiveWorkerOptions> options,
    ILogger<RabbitMqReceiveWorker> logger)
    : BackgroundService
{
    private readonly IConnection _connection =
        connection ?? throw new ArgumentNullException(nameof(connection));
    private readonly IServiceScopeFactory _serviceScopeFactory =
        serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
    private readonly RabbitMqReceiveTopology _receiveTopology =
        receiveTopology ?? throw new ArgumentNullException(nameof(receiveTopology));
    private readonly RabbitMqReceiveWorkerOptions _options = GetValidatedOptions(options);
    private readonly ILogger<RabbitMqReceiveWorker> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public override Task StartAsync(
        CancellationToken ct)
    {
        if (_receiveTopology.QueueNames.Count == 0)
        {
            throw new InvalidOperationException(
                "RabbitMQ receive worker requires at least one receive queue binding.");
        }

        return base.StartAsync(ct);
    }

    protected override async Task ExecuteAsync(
        CancellationToken ct)
    {
        await using IChannel channel = await _connection.CreateChannelAsync(
            options: null,
            cancellationToken: ct);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            _options.PrefetchCount,
            global: false,
            cancellationToken: ct);

        foreach (string queueName in _receiveTopology.QueueNames)
        {
            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += (_, delivery) =>
                HandleDeliveryAsync(channel, queueName, delivery, ct);

            await channel.BasicConsumeAsync(
                queueName,
                autoAck: false,
                consumerTag: string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer,
                cancellationToken: ct);
        }

        await WaitUntilStoppedAsync(ct);
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        string queueName,
        BasicDeliverEventArgs delivery,
        CancellationToken ct)
    {
        try
        {
            await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
            IRabbitMqReceivedMessageHandler handler =
                scope.ServiceProvider.GetRequiredService<IRabbitMqReceivedMessageHandler>();

            await handler.HandleAsync(
                queueName,
                delivery,
                (deliveryTag, acknowledgeCt) =>
                    channel.BasicAckAsync(
                        deliveryTag,
                        multiple: false,
                        cancellationToken: acknowledgeCt),
                ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "RabbitMQ receive worker failed while handling delivery {DeliveryTag} from queue {QueueName}. MessageId: {MessageId}. MessageType: {MessageType}. Exchange: {Exchange}. RoutingKey: {RoutingKey}. Redelivered: {Redelivered}. Bondstone will negatively acknowledge the delivery with requeue {RequeueOnFailure}; RabbitMQ retry and dead-letter policy remain broker-owned.",
                delivery.DeliveryTag,
                queueName,
                delivery.BasicProperties.MessageId,
                delivery.BasicProperties.Type,
                delivery.Exchange,
                delivery.RoutingKey,
                delivery.Redelivered,
                _options.RequeueOnFailure);

            await channel.BasicNackAsync(
                delivery.DeliveryTag,
                multiple: false,
                requeue: _options.RequeueOnFailure,
                cancellationToken: ct);
        }
    }

    private static async Task WaitUntilStoppedAsync(
        CancellationToken ct)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private static RabbitMqReceiveWorkerOptions GetValidatedOptions(
        IOptions<RabbitMqReceiveWorkerOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        RabbitMqReceiveWorkerOptions receiveOptions = options.Value;
        receiveOptions.Validate();
        return receiveOptions;
    }
}
