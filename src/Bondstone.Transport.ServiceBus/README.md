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
app-owned. See
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).

Install this package in hosts that send durable outbox messages to Azure
Service Bus or run opt-in Service Bus receive workers. Projects that only
declare message contracts or module handlers do not need the adapter package.

See:

- [Azure Service Bus transport architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/transport-servicebus.md)
- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
- [Azure Service Bus transport tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Transport.ServiceBus.Tests)
