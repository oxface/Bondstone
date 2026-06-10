# Future Work

This document tracks ideas that are not part of Bondstone's current operating
contract. Move an item into stable docs only after the corresponding decision
is accepted and implemented.

## Compatibility And Packaging

- Evaluate wider target framework support when consumer demand justifies the
  maintenance cost.
- Revisit independent package versioning only if coordinated package versioning
  becomes a release-management problem.
- Consider narrower package splits only with an ADR that preserves the current
  dependency direction.

Active public API cleanup and real-project readiness work are tracked in
[10-public-api-and-composition-cleanup.md](10-public-api-and-composition-cleanup.md)
and [13-real-project-readiness.md](13-real-project-readiness.md).

## Persistence

- Add migration helpers or provider-specific migration conventions.

Active persistence recovery, maintenance, operation-state, payload-storage, and
multi-data-source work is tracked in
[11-persistence-recovery-and-maintenance.md](11-persistence-recovery-and-maintenance.md).
Domain event work is tracked separately in
[09-domain-events.md](09-domain-events.md).

## Transport

- Add external event handoff formats such as unwrapped payloads, CloudEvents,
  schema-specific envelopes, or non-JSON payload negotiation.

Active transport and hosting ergonomics are tracked in
[12-transport-and-hosting-ergonomics.md](12-transport-and-hosting-ergonomics.md).

## Hosting

Active hosting ergonomics are tracked in
[12-transport-and-hosting-ergonomics.md](12-transport-and-hosting-ergonomics.md).

## Samples

Active sample and adoption readiness work is tracked in
[13-real-project-readiness.md](13-real-project-readiness.md).
