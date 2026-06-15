# Bondstone.Transport

Provider-neutral transport diagnostics and topology contracts for Bondstone
durable messaging.

## Quick Path

Most applications should install a concrete transport package such as
`Bondstone.Transport.RabbitMq` or `Bondstone.Transport.ServiceBus` and use its
`AddBondstone` transport extension from
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).
Use this package directly for custom transport adapters or diagnostic tooling.

Install this package when writing provider-neutral transport diagnostics or a
custom outbox transport adapter. Application hosts normally reference a
concrete transport package instead.

See:

- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
