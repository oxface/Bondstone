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

## V2 Release Must Path

Status: Ready for a v2 release PR/checkpoint once verification is green.

Completed for the v2 must path:

- Documentation now points at the active thin RabbitMQ and Azure Service Bus
  adapters rather than saying broker adapter packages still need to be
  reintroduced.
- Stable operations guidance covers direct receive semantics, ambiguous
  unprocessed inbox rows, broker settlement, outbox terminal failures,
  operation finalization and expiration, EF migrations, contract evolution,
  retention, and observability ownership.
- Stable observability guidance documents the current activity sources,
  activities, tags, log event ids, result diagnostics, inspection contracts,
  and non-current metrics/error-code vocabulary.
- Worker and adapter scope is settled for v2: Bondstone owns durable outbox
  dispatch and receive handoff boundaries; applications own broker topology,
  retry, dead-letter, prefetch/concurrency, provisioning, and monitoring.
- RabbitMQ `AutoAck` was removed from the Bondstone receive worker. RabbitMQ
  receive workers use manual acknowledgement and settle only after Bondstone
  durable receive succeeds.
- Azure Service Bus receive rejects `AutoCompleteMessages = true` and
  completes native messages only after Bondstone durable receive succeeds.
- Local transport is documented as explicit sample, test, and local
  development infrastructure, not production broker durability or a hidden
  fallback.
- The public API inventory has a final v2 decision check. Remaining public
  concrete helpers are classified as deliberate normal defaults, advanced
  composition APIs, or provider/runtime concrete APIs rather than unresolved
  implementation-detail exposure.
- Package discovery, package README, setup, operations, observability,
  packaging, and public API docs describe the current active package set and
  v2 replacement posture.
- `docs/packaging.md` contains the v2 release checklist, migration checklist,
  and NuGet registry follow-up actions. Release Please remains the normal
  owner of the actual version bump, changelog, tag, and GitHub release.

Still required in the release PR or release workflow, not in this readiness
checkpoint:

- Release Please must create the actual v2 release PR and update
  `Directory.Build.props`, `.github/.release-please-manifest.json`,
  `CHANGELOG.md`, the version tag, and the GitHub release together.
- Maintainers must verify the package artifacts from the release ref before
  publishing.
- NuGet publish, v1 deprecation, and v1 delisting are explicit post-approval
  registry actions and are not part of this source checkpoint.

## Post-V2 Or Long-Term Work

These items remain intentionally outside the v2 must path:

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

## Deliberately Deferred

- generic application middleware pipeline;
- topology DSLs or provisioning;
- provider-neutral transport diagnostics;
- subscription storage;
- broker retry/dead-letter orchestration;
- saga or workflow engine features;
- non-EF persistence provider work without real consumer demand;
- broad multi-transport routing ergonomics before a concrete use case;
- optional hosted operation expiration worker unless application feedback
  shows it belongs in Bondstone rather than app-owned scheduling.

## Verification

Plan-only change. Read root and ADR guidance, current architecture docs,
setup docs, testing docs, package/public API docs, and the current receive,
outbox, EF transaction, and transport adapter code paths during the
post-MVP review.
