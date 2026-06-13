# Bondstone.Persistence

Provider-neutral durable persistence contracts and outbox/inbox primitives for
Bondstone.

## Quick Path

Most applications should install a concrete persistence provider package and
use its module helper from [../../docs/setup.md](../../docs/setup.md). Use this
package directly for custom persistence, dispatch composition, or tests that
need the provider-neutral contracts.

See [../../docs/architecture/persistence-core.md](../../docs/architecture/persistence-core.md).
