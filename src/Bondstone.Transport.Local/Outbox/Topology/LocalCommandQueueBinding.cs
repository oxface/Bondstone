namespace Bondstone.Transport.Local.Outbox;

internal sealed record LocalCommandQueueBinding(
    string QueueName,
    string ModuleName);
