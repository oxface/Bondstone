# Bondstone.Hosting

Hosted worker composition for Bondstone durable messaging.

This package composes reusable hosting pieces around core durable messaging
contracts. It should not own provider-specific transport behavior or broker
administration.

## Quick Path

Add this package when a host should run Bondstone's durable outbox worker or
the durable incoming inbox processing worker. Normal outbox setup
configures the worker with `bondstone.Outbox.UseWorker(...)` inside
`AddBondstone`; incoming inbox processing uses
`bondstone.UseDurableIncomingInboxWorker(...)`. Provider transport setup still
belongs to the concrete transport packages or the application. See
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).

Install this package in executable hosts that need the built-in outbox
dispatcher worker or incoming inbox processing worker. Module contract
projects and provider-only libraries do not need it unless they configure
hosted processing.

See:

- [Setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
- [Operations guide](https://github.com/oxface/Bondstone/blob/main/docs/operations.md)
- [Observability guide](https://github.com/oxface/Bondstone/blob/main/docs/observability.md)
- [Packaging, release, and migration policy](https://github.com/oxface/Bondstone/blob/main/docs/packaging.md)
- [Hosting architecture](https://github.com/oxface/Bondstone/blob/main/docs/architecture/hosting.md)
- [Persistence contracts](https://github.com/oxface/Bondstone/blob/main/docs/architecture/persistence-core.md)
- [Hosting tests](https://github.com/oxface/Bondstone/tree/main/tests/Bondstone.Hosting.Tests)
