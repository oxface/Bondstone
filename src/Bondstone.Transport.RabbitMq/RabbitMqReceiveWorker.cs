using Bondstone.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Bondstone.Transport.RabbitMq;

internal sealed class RabbitMqReceiveWorker(
    IChannel channel,
    IServiceScopeFactory scopeFactory,
    IEnumerable<RabbitMqReceiveWorkerRegistration> registrations,
    ILogger<RabbitMqReceiveWorker> logger)
    : BackgroundService
{
    private readonly IChannel _channel =
        channel ?? throw new ArgumentNullException(nameof(channel));
    private readonly IServiceScopeFactory _scopeFactory =
        scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly IReadOnlyCollection<RabbitMqReceiveWorkerRegistration> _registrations =
        registrations?.ToArray() ?? throw new ArgumentNullException(nameof(registrations));
    private readonly ILogger<RabbitMqReceiveWorker> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (_registrations.Count == 0)
        {
            return;
        }

        var consumerTags = new List<string>();
        foreach (RabbitMqReceiveWorkerRegistration registration in _registrations)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += (_, args) =>
                ReceiveAsync(args, registration, stoppingToken);

            string consumerTag = await _channel.BasicConsumeAsync(
                queue: registration.QueueName,
                autoAck: registration.AutoAck,
                consumerTag: registration.ConsumerTag ?? string.Empty,
                noLocal: false,
                exclusive: false,
                arguments: null,
                consumer: consumer,
                cancellationToken: stoppingToken);
            consumerTags.Add(consumerTag);
        }

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        finally
        {
            foreach (string consumerTag in consumerTags)
            {
                await _channel.BasicCancelAsync(
                    consumerTag,
                    noWait: false,
                    cancellationToken: CancellationToken.None);
            }
        }
    }

    private async Task ReceiveAsync(
        BasicDeliverEventArgs args,
        RabbitMqReceiveWorkerRegistration registration,
        CancellationToken stoppingToken)
    {
        try
        {
            await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
            IDurableEnvelopeReceiver receiver =
                scope.ServiceProvider.GetRequiredService<IDurableEnvelopeReceiver>();

            await receiver.ReceiveAsync(
                args.Body,
                registration.Binding,
                stoppingToken);

            if (!registration.AutoAck)
            {
                await _channel.BasicAckAsync(
                    args.DeliveryTag,
                    multiple: false,
                    cancellationToken: stoppingToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "RabbitMQ receive worker failed for queue '{QueueName}'.",
                registration.QueueName);

            if (!registration.AutoAck)
            {
                await _channel.BasicNackAsync(
                    args.DeliveryTag,
                    multiple: false,
                    requeue: registration.RequeueOnFailure,
                    cancellationToken: CancellationToken.None);
            }
        }
    }
}
