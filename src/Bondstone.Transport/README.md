# Bondstone.Transport

Provider-neutral transport diagnostics and topology contracts for Bondstone
durable messaging.

## Quick Path

Most applications should install a concrete transport package such as
`Bondstone.Transport.RabbitMq` or `Bondstone.Transport.ServiceBus` and use its
`AddBondstone` transport extension from
[../../docs/setup.md](../../docs/setup.md). Use this package directly for
custom transport adapters or diagnostic tooling.

See [../../docs/architecture/messaging.md](../../docs/architecture/messaging.md).
