# Bondstone

Core abstractions for durable Bondstone module boundaries.

This package owns module registration and execution contracts, durable command
and integration event contracts, payload serialization, module-aware runtime
composition over provider-neutral persistence and transport contracts, and the
lightweight `AddBondstone` composition surface.

## Quick Path

Start here for every Bondstone host, then add hosting, persistence, capability,
and transport packages as needed. Normal application setup should use
`AddBondstone` with module-owned registration helpers as shown in
[../../docs/setup.md](../../docs/setup.md).

See:

- [../../docs/architecture/messaging.md](../../docs/architecture/messaging.md)
- [../../docs/architecture/modules.md](../../docs/architecture/modules.md)
- [../../docs/architecture/persistence-core.md](../../docs/architecture/persistence-core.md)
- [../../tests/Bondstone.Tests](../../tests/Bondstone.Tests)
