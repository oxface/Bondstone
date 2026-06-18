# 0019 Operation Observation Not Orchestration

Status: Proposed
Application: Not Applicable
Date: 2026-06-18

## Context

Bondstone has operation state and result APIs for durable commands. The
post-MVP review initially emphasized non-throwing wait ergonomics, but design
discussion clarified that waiting for durable command results can easily be
mistaken for orchestration.

Inside a module handler, waiting for another durable command to complete would
hold work open across an asynchronous boundary. If the process crashes, there
is no persisted continuation to resume unless Bondstone owns saga or workflow
state. Adding that safely requires correlation, persisted process state,
timeouts, compensation/failure policy, idempotent resume handling, inspection,
and cleanup.

For v2, Bondstone should not ship a partial saga feature under operation-result
terminology.

## Decision

Operation state and operation results should be positioned as observation and
edge-facing result retrieval, not orchestration.

Durable command tracking should support:

- durable ingress creating and returning an operation id;
- durable command sending carrying an operation id supplied by the caller;
- target module durable inbox processing finalizing operation state/result in
  the target module persistence boundary;
- reading operation status/result later by operation id;
- optional short bounded waiting for HTTP/UI/test ergonomics.

Operation waiting is not a saga primitive. Bondstone should not recommend
waiting for operation results inside module handlers. Bondstone should not add
automatic operation-result subscriptions, polling another module's operation
store from a caller module, generic command-completion events, or automatic
operation chaining in v2.

Durable inter-module continuations should be expressed with explicit
integration events and app-owned process state until a later accepted saga or
process-manager ADR introduces a native abstraction.

## Consequences

The operation model remains valuable for accepted-work APIs, diagnostics, UI
refresh loops, and externally visible completion status.

The v2 docs must stop presenting operation waiting as the central command
result story. The stronger public concept is operation observation:
accepted, pending, completed, failed/finalized, expired, timed out while
waiting, and result deserialization failure.

Applications can still compose their own short-lived aggregators outside a
module transaction, but Bondstone will not document that as durable
orchestration. If the coordination must survive restart, the application must
persist process state or use a workflow/saga engine.

This defers Bondstone-owned sagas/process managers to v3 or a later v2 feature
after real consumer evidence.

## Related Decisions

- Narrows
  [0004 Persistence Operation State And Results](0004-persistence-operation-state-and-results.md).
- Reaffirms
  [0007 Keep Orchestration App Owned For Now](0007-keep-orchestration-app-owned-for-now.md).
- Narrows
  [0016 Non-Throwing Operation Wait Ergonomics](0016-non-throwing-operation-wait-ergonomics.md).
- Relates to
  [0018 V2 Module Execution And Durable Inbox Reset](0018-v2-module-execution-and-durable-inbox-reset.md).

## Application Notes

- Current contract: not binding until accepted. Current APIs still expose
  wait-oriented names introduced during operation-result hardening.
- Stable docs: when accepted, apply to messaging, operations, setup,
  package-discovery, public-api, and samples docs.
- Agent guidance: no agent instruction change yet.
- Application evidence: operation state/finalization/result reader APIs exist,
  but their v2 positioning and naming are not yet cleaned up.
- Pending or deferred: operation API review, docs cleanup, sample update, and
  explicit saga/process-manager backlog item.

## Verification

No executable verification. This ADR records a proposed scope boundary from
the 2026-06-18 orchestration discussion.
