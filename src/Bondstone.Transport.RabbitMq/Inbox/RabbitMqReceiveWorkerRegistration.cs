namespace Bondstone.Transport.RabbitMq.Inbox;

internal sealed record RabbitMqReceiveWorkerRegistration(
    Action<RabbitMqReceiveWorkerOptions>? ConfigureOptions);
