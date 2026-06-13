# Bondstone.Transport.Local

Local in-process transport adapter for Bondstone durable messaging samples and
tests.

This package routes durable messages through provider-neutral receive pipelines
without pretending to provide production broker durability.

## Quick Path

Use this package for samples, tests, and local development that need explicit
in-process queue routing. Production-oriented hosts should use a direct
broker adapter such as `Bondstone.Transport.RabbitMq` or
`Bondstone.Transport.ServiceBus`.

See:

- [../../docs/architecture/transport-local.md](../../docs/architecture/transport-local.md)
- [../../docs/architecture/messaging.md](../../docs/architecture/messaging.md)
- [../../tests/Bondstone.Transport.Local.Tests](../../tests/Bondstone.Transport.Local.Tests)
