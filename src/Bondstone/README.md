# Bondstone

Core abstractions for durable Bondstone module boundaries.

This package owns module registration and execution contracts, durable command
and integration event contracts, payload serialization, module-aware runtime
composition over provider-neutral persistence and transport contracts, and the
lightweight `AddBondstone` composition surface.

## Quick Path

Start here for every Bondstone host, then add hosting, persistence, and
transport packages as needed. Normal application setup should use
`AddBondstone` with module-owned registration helpers as shown in
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).

Install this package in projects that declare Bondstone modules, commands,
integration events, handlers, durable send/publish calls, or in-process module
execution. Provider, hosting, and transport packages are installed separately
only when the project calls their setup APIs.

See:

- [Messaging architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/messaging.md)
- [Module architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/modules.md)
- [Persistence contracts](https://github.com/oxface/Bondstone/blob/main/docs/architecture/persistence-core.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
- [Core tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Tests)
