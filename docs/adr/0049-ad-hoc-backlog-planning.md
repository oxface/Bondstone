# 0049 Ad Hoc Backlog Planning

Status: Accepted
Application: Applied
Date: 2026-06-11

## Context

Bondstone's `docs/backlog` folder grew from focused review campaigns into a
multi-item implementation roadmap. That made the folder expensive to maintain:
every accepted ADR, implementation slice, or strategic pivot required updating
many non-binding backlog files so they did not look like current operating
guidance.

The repository already has stable docs for current behavior and ADRs for
durable decisions. Long-lived backlog files should not become a shadow product
management system. Bondstone needs a lighter planning model that captures
known pressure points while letting maintainers extract one concrete issue at
a time.

The module pipeline and Domain Events work also exposed a better workflow:
implementation evidence should update ADRs and stable docs, while unresolved
ideas should return to a simple plan note instead of staying as partially
maintained backlog tracks.

## Decision

Bondstone will treat `docs/backlog` as ad hoc planning, not a maintained
roadmap.

The folder will contain:

- `00-plans.md`, a loose list of known pressure points and possible future
  work;
- zero or more active issue notes for the immediate work being explored or
  implemented.

Backlog issue notes are temporary. When an issue is resolved, durable
decisions move into ADRs, current behavior moves into stable docs, and any
remaining ideas move back into `00-plans.md`. Resolved issue notes should be
removed unless they preserve useful short-term context that has not yet moved
elsewhere.

Backlog files must not use detailed status matrices as if they are a product
backlog. They may contain enough context, questions, and verification notes to
guide the next slice.

Historical backlog archives may remain under `docs/backlog/archive`, but they
are traceability only. They are not maintained and must not steer new
implementation.

## Consequences

Planning maintenance becomes lighter. Agents should stop sweeping old backlog
tracks after every change and should instead update stable docs, ADRs, and the
one active issue note they are actually working on.

Maintainers must extract new issue notes deliberately from `00-plans.md` when
work becomes concrete. This keeps immediate work visible without pretending
future sequencing is settled.

The old numbered active backlog tracks are consolidated into `00-plans.md`.
Current immediate work is extracted as module runtime isolation because recent
Domain Events implementation exposed same-host module isolation risk.

## Related Decisions

- [0001 Adopt ADR-Led Maintenance](0001-adopt-adr-led-maintenance.md)
- [0048 Reference-Based Context Structure](0048-reference-based-context-structure.md)

## Application Notes

- Current contract: `docs/backlog` is an ad hoc planning area with
  `00-plans.md` plus active issue notes. It is not a maintained implementation
  roadmap.
- Stable docs: applied to [docs/README.md](../README.md),
  [docs/repository.md](../repository.md), and
  [docs/backlog/README.md](../backlog/README.md).
- Agent guidance: applied to root [AGENTS.md](../../AGENTS.md) and
  [docs/backlog/AGENTS.md](../backlog/AGENTS.md).
- Application evidence: previous active backlog tracks were consolidated into
  `00-plans.md`; the immediate next issue is
  `01-module-runtime-isolation.md`; historical backlog archives remain
  traceability-only.
- Pending or deferred: none for the planning model.

## Verification

- `pnpm format:check`
