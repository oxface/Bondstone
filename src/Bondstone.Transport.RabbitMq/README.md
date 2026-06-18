# Bondstone.Transport.RabbitMq

Thin RabbitMQ adapter for Bondstone durable envelopes.

This package provides outbound `IDurableEnvelopeDispatcher` registration and
opt-in receive workers over `RabbitMQ.Client`. It does not declare exchanges,
queues, bindings, retry policies, dead-letter exchanges, credentials,
prefetch/concurrency strategy, or monitoring.

Use it only when the host already owns RabbitMQ topology and wants small
Bondstone envelope plumbing.

The receive worker defaults to direct module receive. It can also be explicitly
configured to ingest native deliveries into the optional durable incoming inbox
ledger with `IngestCommandToDurableIncomingInbox()` or
`IngestEventToDurableIncomingInbox(...)`. In that mode the worker commits the
incoming inbox row before RabbitMQ acknowledgement; the separate
`Bondstone.Hosting` durable incoming inbox worker processes the row later.

See:

- [Setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
- [Operations guide](https://github.com/oxface/Bondstone/blob/main/docs/operations.md)
- [Observability guide](https://github.com/oxface/Bondstone/blob/main/docs/observability.md)
- [Packaging, release, and migration policy](https://github.com/oxface/Bondstone/blob/main/docs/packaging.md)
- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
