# Bondstone.Transport.ServiceBus

Thin Azure Service Bus adapter for Bondstone durable envelopes.

This package provides outbound `IDurableEnvelopeDispatcher` registration and
opt-in receive workers over `Azure.Messaging.ServiceBus`. It does not create
queues, topics, subscriptions, rules, retry policies, dead-letter policy,
credentials, or monitoring.

Use it only when the host already owns Azure Service Bus topology and wants
small Bondstone envelope plumbing.

See:

- [Setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
- [Operations guide](https://github.com/oxface/Bondstone/blob/main/docs/operations.md)
- [Observability guide](https://github.com/oxface/Bondstone/blob/main/docs/observability.md)
- [Packaging, release, and migration policy](https://github.com/oxface/Bondstone/blob/main/docs/packaging.md)
- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
