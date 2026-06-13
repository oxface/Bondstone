# Bondstone.Transport.ServiceBus

Azure Service Bus transport adapter integration for Bondstone durable
messaging.

This package owns Service Bus-specific outbox sending, receive topology,
message mapping, settlement helpers, and opt-in hosted receive worker
composition.

## Quick Path

Use this package when a host dispatches Bondstone durable outbox records
through Azure Service Bus. Normal setup calls
`bondstone.UseServiceBusTransport(...)` inside `AddBondstone`; namespaces,
queues, topics, subscriptions, retry policy, and dead-letter behavior remain
app-owned. See [../../docs/setup.md](../../docs/setup.md).

See:

- [../../docs/architecture/transport-servicebus.md](../../docs/architecture/transport-servicebus.md)
- [../../docs/architecture/messaging.md](../../docs/architecture/messaging.md)
- [../../tests/Bondstone.Transport.ServiceBus.Tests](../../tests/Bondstone.Transport.ServiceBus.Tests)
