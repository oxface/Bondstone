# Bondstone.Transport.Local

Local in-process transport adapter for Bondstone durable messaging samples and
tests.

This package routes durable messages through provider-neutral receive pipelines
without pretending to provide production broker durability.

## Quick Path

Use this package for samples, tests, and local development that need explicit
in-process queue routing. `UseModuleQueueConvention()` is the normal
modular-monolith command path; explicit command routes also need matching
`Queue(...).AcceptModule(...)` bindings. Integration events need explicit
subscriber bindings with `Queue(...).SubscribeEvent(...)` because subscriber
module and subscriber identity are part of the inbox key.

Production-oriented broker hosts should keep native transport code app-owned
and bridge through `IDurableEnvelopeDispatcher`,
`IDurableMessageEnvelopeSerializer`, and `IDurableEnvelopeReceiver`.

Install this package only when the host intentionally routes durable messages
through local queues and Bondstone receive pipelines. It is not a production
broker fallback and does not provide broker durability, topology management,
retry, dead-letter handling, or receive-buffer behavior.

See:

- [Setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
- [Operations guide](https://github.com/oxface/Bondstone/blob/main/docs/operations.md)
- [Observability guide](https://github.com/oxface/Bondstone/blob/main/docs/observability.md)
- [Packaging, release, and migration policy](https://github.com/oxface/Bondstone/blob/main/docs/packaging.md)
- [Local transport architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/transport-local.md)
- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
- [Local transport tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Transport.Local.Tests)
