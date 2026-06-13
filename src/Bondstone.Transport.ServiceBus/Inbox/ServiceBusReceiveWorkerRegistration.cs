namespace Bondstone.Transport.ServiceBus.Inbox;

internal sealed record ServiceBusReceiveWorkerRegistration(
    Action<ServiceBusReceiveWorkerOptions>? ConfigureOptions);
