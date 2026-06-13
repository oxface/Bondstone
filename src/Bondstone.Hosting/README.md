# Bondstone.Hosting

Hosted worker composition for Bondstone durable messaging.

This package composes reusable hosting pieces around core durable messaging
contracts. It should not own provider-specific transport behavior or broker
administration.

## Quick Path

Add this package when a host should run Bondstone's durable outbox worker.
Normal setup configures the worker with `bondstone.Outbox.UseWorker(...)`
inside `AddBondstone`; provider transport setup still belongs to the concrete
transport packages. See [../../docs/setup.md](../../docs/setup.md).

See:

- [../../docs/architecture/hosting.md](../../docs/architecture/hosting.md)
- [../../docs/architecture/persistence-core.md](../../docs/architecture/persistence-core.md)
- [../../tests/Bondstone.Hosting.Tests](../../tests/Bondstone.Hosting.Tests)
