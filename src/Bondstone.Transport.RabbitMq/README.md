# Bondstone.Transport.RabbitMq

Thin RabbitMQ adapter for Bondstone durable envelopes.

This package provides outbound `IDurableEnvelopeDispatcher` registration and
opt-in receive workers over `RabbitMQ.Client`. It does not declare exchanges,
queues, bindings, retry policies, dead-letter exchanges, credentials,
prefetch/concurrency strategy, or monitoring.

Use it only when the host already owns RabbitMQ topology and wants small
Bondstone envelope plumbing.

See:

- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
