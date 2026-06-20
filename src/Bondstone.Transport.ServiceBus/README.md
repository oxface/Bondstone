# Bondstone.Transport.ServiceBus

Thin Azure Service Bus adapter for Bondstone durable envelopes.

This package provides outbound `IDurableEnvelopeDispatcher` registration and
opt-in receive workers over `Azure.Messaging.ServiceBus`. It does not create
queues, topics, subscriptions, rules, retry policies, dead-letter policy,
credentials, or monitoring.

Use it only when the host already owns Azure Service Bus topology and wants
small Bondstone envelope plumbing.

The receive worker ingests native deliveries into the durable incoming inbox
ledger with `ReceiveCommand()` or `ReceiveEvent(...)`. It requires manual
completion by keeping `AutoCompleteMessages = false` and `ReceiveMode =
PeekLock`, commits the incoming inbox row before Azure Service Bus message
completion, and leaves the separate `Bondstone.Hosting` durable incoming inbox
worker to process the row later.

See:

- [Setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
- [Operations guide](https://github.com/oxface/Bondstone/blob/main/docs/operations.md)
- [Observability guide](https://github.com/oxface/Bondstone/blob/main/docs/observability.md)
- [Packaging, release, and migration policy](https://github.com/oxface/Bondstone/blob/main/docs/packaging.md)
- [BMAD architecture](https://github.com/oxface/Bondstone/blob/main/_bmad-output/planning-artifacts/architecture.md)
