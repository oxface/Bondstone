# 0057 Operation State Operational Semantics

Status: Accepted
Application: Applied
Date: 2026-06-16

## Context

Bondstone already persists durable operation state for caller-supplied
operation ids. The current command loop writes `Pending` when a durable command
is accepted for send, and writes `Completed` plus an optional result payload
and diagnostic context when a durable result command completes in the target
module receive boundary.

Post-publication feedback and architecture review exposed the next operational
gap: callers can poll for a durable result, but a command that never reaches a
successful target-module commit can remain `Pending` forever unless the
application writes terminal state manually. The temptation is to infer
operation failure from transport retry exhaustion, inbox rows, or outbox
terminal failures. That would make operation state look more complete, but it
would also mix several different ledgers:

- outbox rows describe source-side dispatch attempts and terminal dispatch
  failure;
- inbox rows describe target-side idempotency and processed markers;
- broker or app-owned transport runtime describes provider-native delivery,
  retry, and dead-letter behavior; and
- operation state describes the caller-visible workflow outcome for one
  durable operation id.

Bondstone is also simplifying transport ownership after MVP. The library is no
longer trying to own provider-neutral topology or broker retry semantics.
Operation-state policy must fit that smaller library shape and avoid
recreating a hidden broker workflow engine.

## Decision

Bondstone should keep operation state as a caller-visible workflow/result read
model, not as the delivery ledger.

The operation-state ownership model should be:

- source module persistence owns the acceptance receipt for durable send;
- target module persistence owns committed command outcome and result payload;
- outbox persistence owns outgoing dispatch attempts and terminal dispatch
  failure;
- inbox persistence owns receive idempotency and processed markers; and
- broker or app transport runtime owns provider-native delivery exhaustion.

`Pending` means the durable operation was accepted for work but no terminal
target outcome is observable yet. Bondstone may stage this state during durable
send when the caller supplies an operation id.

`Running` is optional and should be written only when Bondstone or application
code can safely make the transition inside the target module persistence
boundary. It must not be inferred from outbox dispatch alone.

`Completed` means the target module command committed successfully. Bondstone
may stage this state inside the target module transaction, including serialized
result payload and diagnostic context for result commands, but the state only
becomes durable evidence after the surrounding target-module transaction
commits.

`Failed` means a clear Bondstone or application policy has determined the
operation will not complete successfully. Transient handler exceptions, broker
redelivery, already-received unprocessed inbox rows, and source outbox dispatch
failure do not automatically prove operation failure.

`Cancelled` means an application-owned cancellation policy has produced a
terminal cancellation outcome.

The next implementation direction should add an application-facing operation
state writer or finalization API for explicit `Failed` and `Cancelled`
outcomes. That API should be higher-level than the low-level
`IDurableOperationStateStore`, preserve the current operation-state status
precedence rules, and keep result-reader diagnostics useful.

The first timeout/expiry direction should be explicit application policy:
applications configure which operations are allowed to expire, what age or
deadline makes them expired, and whether expiry writes `Failed` or
`Cancelled`. A maintenance job can apply that policy by marking stale
`Pending` or `Running` operations terminal with a reason. Hosting may provide a
reusable worker shape later, but provider stores should own any efficient
claim/update primitives needed for their database.

Bondstone should not add a provider-neutral receive retry ledger or infer
operation `Failed` from broker dead-letter behavior in the next slice.
Provider-specific dead-letter handoff helpers may be added later only when a
provider can expose an unambiguous final-delivery outcome without duplicating
broker retry policy.

Operation reads should eventually gain module-aware hints or handles so common
result observation does not need to query every module store. Global operation
reads may remain compatibility behavior for small hosts, tests, and callers
that only have an operation id.

## Consequences

Operation state remains understandable: it answers "what outcome should the
caller observe for this durable operation id?" rather than trying to explain
every outbox, inbox, and broker transition.

Polling APIs can become safer for real applications once explicit failure,
cancellation, and expiry policies are available. A caller will be able to see a
terminal non-success state instead of polling forever for workflows the
application has declared expired or failed.

Bondstone does not hide transport complexity behind a false provider-neutral
failure signal. Broker retry and dead-letter behavior stay provider-native and
application-owned unless a later provider-specific helper proves a narrow
handoff.

The model leaves some work intentionally deferred: operation finalization API
shape, timeout policy configuration, efficient provider primitives for stale
operation queries, module-aware operation handles or hints, and operator
guidance for outbox and inbox recovery.

## Related Decisions

- [0031 Durable Operation State Integration](0031-durable-operation-state-integration.md)
- [0038 Provider Retry Recovery And Settlement Boundaries](0038-provider-retry-recovery-and-settlement-boundaries.md)
- [0054 Result-Returning Command Model](0054-result-returning-command-model.md)
- [0056 Post-MVP Communication And Transport Simplification](0056-post-mvp-communication-and-transport-simplification.md)

## Application Notes

- Current contract: Current runtime writes `Pending` and `Completed` in the
  command loop. Applications can explicitly mark operations `Failed` or
  `Cancelled` for a module-owned operation-state store through
  `IDurableOperationFinalizer`. `Running` remains a storage/read-model value
  for application-owned policy.
- Stable docs: Current behavior is documented in
  [docs/architecture/messaging.md](../architecture/messaging.md),
  [docs/architecture/persistence-core.md](../architecture/persistence-core.md),
  [docs/package-discovery.md](../package-discovery.md), and
  [docs/setup.md](../setup.md).
- Agent guidance: Root [AGENTS.md](../../AGENTS.md) already requires ADR
  review before changing durable behavior, provider behavior, transport
  behavior, public API, or compatibility policy.
- Application evidence: `IDurableOperationFinalizer` and
  `DurableOperationFinalizationResult` provide the first application-facing
  finalization API. The default implementation writes `Failed` or `Cancelled`
  to the named module's operation-state store, preserves existing terminal
  state, and keeps result-reader diagnostics compatible.
  `IDurableOperationExpirationProcessor` and
  `IDurableOperationExpirationStore` provide the first reusable app-owned
  expiry pass: provider stores find stale `Pending` or `Running` candidates,
  and the processor finalizes them through the finalizer.
- Pending or deferred: Hosted expiry workers, provider-specific bulk mutation
  primitives, provider-specific dead-letter handoff helpers, module-aware
  operation handles, and operator recovery guidance remain separate follow-up
  work.

## Verification

Created and applied this ADR after reading:

- root and ADR workflow guidance through the ADR create skill;
- current architecture docs for messaging, core persistence, EF persistence,
  and PostgreSQL persistence;
- ADR 0031, ADR 0054, and ADR 0056; and
- the post-MVP architecture and consumer feedback plan.

Verified application with focused operation finalizer tests, public API
baseline refresh/check, build, fast tests, formatting, package verification,
and `git diff --check`.
