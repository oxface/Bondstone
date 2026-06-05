# Extraction

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

## Application State

This extraction strategy is accepted and documented. The first `Bondstone`
core slice has started with stable message identity contracts and the message
type registry, durable command send contracts, durable operation read
contracts, durable message envelopes, and initial persistence-neutral outbox
and inbox contracts, including operation-state storage. EF Core
provider-neutral entities, model mappings, registration helpers, and store
implementations have started. `Send and wait` behavior, trace context and
causation propagation, retry policy, broader PostgreSQL provider behavior,
Rebus transport behavior, additional integration tests, and samples remain
future extraction work. PostgreSQL integration tests have started with
Testcontainers-backed verification of the provider-neutral EF Core mappings and
stores against a real database, plus provider-specific registration and
constraint/unique-violation classification helpers. PostgreSQL savepoint
rollback and `FOR UPDATE SKIP LOCKED` primitives have been verified for future
inbox and outbox APIs. Outbox dispatch state includes provider-neutral claim
lease fields, and a narrow `IDurableOutboxClaimer` contract now has a
PostgreSQL implementation for claiming due rows, scheduled rows, and expired
processing leases. PostgreSQL registration passes the mapped schema to
provider-owned SQL when a non-default schema is configured. A narrow
`IDurableOutboxDispatchRecorder` contract now has a PostgreSQL implementation for
recording dispatch success, retry scheduling, and dead-letter outcomes after a
claimed delivery attempt. A narrow `IDurableInboxRegistrar` contract now has a
PostgreSQL implementation for idempotent receive-side registration and
duplicate classification. Dispatcher loops, transport send implementation,
lease renewal, retry-delay calculation, max-attempt policy, stale-claim
recovery, dead-letter routing, inbox handler orchestration, receive-side retry,
transport acknowledgement, and broader transport behavior remain future
extraction work. General in-process module calls are not an extraction target
unless a later ADR or sample exposes a durable boundary need. Do not extract
the historical generic mediator/message-bus layer as a default Bondstone
feature.
