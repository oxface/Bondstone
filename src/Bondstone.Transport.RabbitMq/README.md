# Bondstone.Transport.RabbitMq

RabbitMQ transport adapter integration for Bondstone durable messaging.

This package owns RabbitMQ-specific outbox publishing, receive topology,
message mapping, settlement helpers, and opt-in hosted receive worker
composition.

## Quick Path

Use this package when a host dispatches Bondstone durable outbox records
through RabbitMQ. Normal setup registers the RabbitMQ connection and calls
`bondstone.UseRabbitMqTransport(...)` inside `AddBondstone`; broker entities,
bindings, retry policy, and dead-letter behavior remain app-owned. See
[../../docs/setup.md](../../docs/setup.md).

See:

- [../../docs/architecture/transport-rabbitmq.md](../../docs/architecture/transport-rabbitmq.md)
- [../../docs/architecture/messaging.md](../../docs/architecture/messaging.md)
- [../../tests/Bondstone.Transport.RabbitMq.Tests](../../tests/Bondstone.Transport.RabbitMq.Tests)
