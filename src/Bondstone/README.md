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

- [Setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
- [Operations guide](https://github.com/oxface/Bondstone/blob/main/docs/operations.md)
- [Observability guide](https://github.com/oxface/Bondstone/blob/main/docs/observability.md)
- [Packaging, release, and migration policy](https://github.com/oxface/Bondstone/blob/main/docs/packaging.md)
- [BMAD architecture](https://github.com/oxface/Bondstone/blob/main/_bmad-output/planning-artifacts/architecture.md)
- [Core tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Tests)
