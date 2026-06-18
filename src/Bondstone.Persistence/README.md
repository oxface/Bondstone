# Bondstone.Persistence

Provider-neutral durable persistence contracts and outbox/inbox primitives for
Bondstone.

## Quick Path

Most applications should install a concrete persistence provider package and
use its module helper from
[the setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md).
Use this package directly for custom persistence, dispatch composition, or
tests that need the provider-neutral contracts.

Install this package when implementing or composing custom durable outbox,
inbox, operation-state, dispatcher, or transport persistence behavior. Normal
EF Core or PostgreSQL applications usually get it transitively from a concrete
provider package.

See:

- [Setup guide](https://github.com/oxface/Bondstone/blob/main/docs/setup.md)
- [Package discovery](https://github.com/oxface/Bondstone/blob/main/docs/package-discovery.md)
- [Operations guide](https://github.com/oxface/Bondstone/blob/main/docs/operations.md)
- [Observability guide](https://github.com/oxface/Bondstone/blob/main/docs/observability.md)
- [Packaging, release, and migration policy](https://github.com/oxface/Bondstone/blob/main/docs/packaging.md)
- [BMAD architecture](https://github.com/oxface/Bondstone/blob/main/_bmad-output/planning-artifacts/architecture.md)
