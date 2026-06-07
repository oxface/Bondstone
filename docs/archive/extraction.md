# Extraction

Archived: this document is preserved for historical context only. Use
[../mvp-plan.md](../mvp-plan.md) for current implementation planning.

Bondstone is extracted slowly, project by project and often layer by layer.

## Purpose

The extraction is not a compatibility-preservation exercise for the current
consumer repository. Bondstone may break, rename, split, or rewrite current
APIs when that produces a better reusable library.

The current consumer can be adapted later, replaced, or archived. Its current
compatibility should not block Bondstone design.

## Approach

Do not bulk-copy all source into this repository as one migration step.

Each extraction step should move or rewrite a coherent slice and should be
small enough to review:

- package boundaries;
- public surface;
- dependency direction;
- tests;
- docs;
- migration risks;
- service-extraction assumptions;
- usefulness for new consumers.

Prefer this broad extraction order unless a later ADR changes it:

1. Core abstractions and low-dependency primitives.
2. Durable command and messaging contracts.
3. Message identity and registration behavior.
4. EF Core-neutral persistence abstractions and entities.
5. PostgreSQL provider behavior.
6. Rebus transport behavior.
7. Integration tests rewritten around neutral Bondstone fixtures.
8. Samples that validate modular-monolith and service-split usage.

Each step should do at least one of these:

- move a coherent code slice;
- rewrite tests into neutral fixtures;
- create or update stable docs;
- create or update ADRs when design tension appears;
- identify package, new-consumer compatibility, or service-extraction
  questions before moving further.
- record intentional source terminology or API renames in the tactical
  extraction plan while the slice is active.

Avoid destructive edits in the current consumer repository unless they are part
of an explicit migration step.

## Current Status

This extraction strategy was accepted for the early repository phase. Current
implementation scope is tracked in [../mvp-plan.md](../mvp-plan.md). The
historical tactical backlog is preserved in
[extraction-plan.md](extraction-plan.md).

General in-process module calls are not an extraction target unless a later ADR
or sample exposes a durable boundary need. Do not extract the historical
generic mediator/message-bus layer as a default Bondstone feature.
