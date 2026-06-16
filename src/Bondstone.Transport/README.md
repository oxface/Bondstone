# Bondstone.Transport

Provider-neutral transport diagnostics and topology contracts for Bondstone
durable messaging.

## Quick Path

Most applications should install the concrete `Bondstone.Transport.RabbitMq`
transport package when using the remaining direct broker adapter, or use
`Bondstone.Transport.Local` for samples, tests, and local development. Use the
transport extension from
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).
Use this package directly for custom transport adapters or diagnostic tooling.

Install this package when writing provider-neutral transport diagnostics or a
custom durable envelope dispatch adapter. Application hosts normally reference
a concrete transport package instead.

See:

- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
