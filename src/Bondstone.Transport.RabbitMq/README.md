# Bondstone.Transport.RabbitMq

RabbitMQ transport adapter integration for Bondstone durable messaging.

This package owns RabbitMQ-specific outbox publishing, receive bindings,
message mapping, settlement helpers, and opt-in hosted receive worker
composition.

## Quick Path

Use this package when a host dispatches Bondstone durable outbox records
through RabbitMQ. Normal setup registers the RabbitMQ connection and calls
`bondstone.UseRabbitMqTransport(...)` inside `AddBondstone`; broker entities,
bindings, retry policy, and dead-letter behavior remain app-owned. See
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).

Install this package in hosts that publish durable outbox messages to RabbitMQ
or run opt-in RabbitMQ receive workers. Projects that only declare message
contracts or module handlers do not need the adapter package.

See:

- [RabbitMQ transport architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/transport-rabbitmq.md)
- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
- [RabbitMQ transport tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Transport.RabbitMq.Tests)
