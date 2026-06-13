# 0048 Reference-Based Context Structure

Status: Accepted
Application: Applied
Date: 2026-06-10

## Context

Bondstone has enough source packages, test boundaries, samples, architecture
docs, ADR workflows, and backlog material that a single root agent instruction
file either becomes too large or omits useful local context.

Agents also tend to ingest scoped instruction files automatically. If those
files duplicate architecture details, they increase token load and can drift
from stable docs. If they are too sparse, agents miss package-specific docs and
verification entrypoints.

The repository needs a durable convention for local context indexes that helps
humans and agents find the right docs without turning every task into a full
repository read.

## Decision

Use a reference-based context structure.

Significant repository folders should have local indexes:

- `AGENTS.md` for agents working in that folder;
- `README.md` for humans navigating that folder.

Local indexes should usually be short. They should describe the folder's scope
and link to the stable docs, ADR workflows, package README files, tests, and
verification entrypoints that are relevant for that folder. They should not
duplicate durable architecture, package, testing, or workflow rules unless the
local instruction is unusually important and repeated agent misses justify the
duplication.

Stable docs remain the operating contract. ADRs remain the decision trail.
Scoped `AGENTS.md` and `README.md` files are routing indexes into that material.

## Consequences

Agents can load less automatic context and follow references only when a task
touches that area.

Humans get package- and test-boundary entrypoints without having to infer the
right docs from root files.

Maintainers must keep local indexes reference-oriented. Adding broad
architecture prose to scoped `AGENTS.md` files works against this decision.

When a new significant package, test boundary, sample, docs area, or agent
workflow folder is added, it should get a small local index or an explicit
reason why the root index is enough.

## Related Decisions

- [0006 Testing Strategy](0006-testing-strategy.md)
- [0036 Direct Transport Adapters And Rebus Removal](0036-direct-transport-adapters-and-rebus-removal.md)
- [0046 Public API Surface Policy](0046-public-api-surface-policy.md)

## Application Notes

- Current contract: local `AGENTS.md` and `README.md` files are reference
  indexes. Stable docs carry the durable rules.
- Stable docs: applied in [repository.md](../repository.md).
- Agent guidance: applied through root and scoped `AGENTS.md` files.
- Application evidence: local indexes exist for docs, architecture, ADR,
  backlog, source packages, tests, test purpose areas, samples, repository
  automation, devcontainer, and agent support. The Modular Monolith sample uses
  the samples root index rather than per-module indexes.
- Pending or deferred: none.

## Verification

- `pnpm format:check`
