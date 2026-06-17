# Post-MVP Architecture Review Plan

Date: 2026-06-16

## Purpose

This plan prioritizes the architecture work identified after Bondstone's
public MVP publication and initial consumer feedback.

The plan is intentionally a planning handoff, not a durable architecture
contract. Use the accepted ADRs below as decision boundaries while moving
implementation tasks into GitHub Issues or GitHub Projects.

## Accepted ADRs

- [0011 OTel Native Diagnostics And Misconfiguration Reporting](../adr/0011-otel-native-diagnostics-and-misconfiguration-reporting.md)
- [0012 Direct Receive Inbox And Durable Receive Buffer](../adr/0012-direct-receive-inbox-and-durable-receive-buffer.md)
- [0013 Worker Boundaries And Transport Adapter Ownership](../adr/0013-worker-boundaries-and-transport-adapter-ownership.md)
- [0014 Production Operations And Lifecycle Guidance](../adr/0014-production-operations-and-lifecycle-guidance.md)
- [0015 Service Extraction Proof Before Broad Bus Features](../adr/0015-service-extraction-proof-before-broad-bus-features.md)
- [0016 Non-Throwing Operation Wait Ergonomics](../adr/0016-non-throwing-operation-wait-ergonomics.md)

## After V2

- Durable receive buffer design and implementation, including persistence
  records, leases, retry attempts, terminal receive failure state, worker
  options, retention, and tests.
- Finalized metrics and a stable metric instrument vocabulary for outbox,
  receive, inbox, operation finalization, and operation expiration outcomes.
- Stable misconfiguration error codes. Current exception messages remain
  diagnostic surfaces, but they are not a machine-readable error-code
  vocabulary.
- .NET package validation or ApiCompat against the latest stable v2 package
  after v2 ships, while keeping the current `PublicApiGenerator` baseline test
  as the human-readable public surface review tool.
- Service extraction proof expansion beyond the current local modular
  monolith path and thin adapter proofs, including broader extraction
  scenarios, route-aware multi-transport ergonomics, and a two-transport
  sample only when real demand appears.
- optional hosted operation expiration worker unless application feedback
  shows it belongs in Bondstone rather than app-owned scheduling.

## Verification

Executed v2 work was moved into stable docs and the application notes of the
accepted ADRs linked above. This plan now carries only after-v2 handoff items.
