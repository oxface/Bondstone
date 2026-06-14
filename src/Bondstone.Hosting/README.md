# Bondstone.Hosting

Hosted worker composition for Bondstone durable messaging.

This package composes reusable hosting pieces around core durable messaging
contracts. It should not own provider-specific transport behavior or broker
administration.

## Quick Path

Add this package when a host should run Bondstone's durable outbox worker.
Normal setup configures the worker with `bondstone.Outbox.UseWorker(...)`
inside `AddBondstone`; provider transport setup still belongs to the concrete
transport packages. See
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).

Install this package in executable hosts that need the built-in outbox
dispatcher worker. Module contract projects and provider-only libraries do not
need it unless they configure hosted outbox processing.

See:

- [Hosting architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/hosting.md)
- [Persistence contracts](https://github.com/oxface/Bondstone/blob/main/docs/architecture/persistence-core.md)
- [Hosting tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Hosting.Tests)
