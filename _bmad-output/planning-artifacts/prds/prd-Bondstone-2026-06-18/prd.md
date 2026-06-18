---
title: Bondstone BMAD-Native Documentation And V2 Library Reset
status: final
created: 2026-06-18
updated: 2026-06-18
workflowType: prd
sourceDocuments:
  - README.md
  - AGENTS.md
  - docs/README.md
  - docs/setup.md
  - docs/operations.md
  - docs/observability.md
  - docs/packaging.md
  - docs/public-api.md
  - docs/repository.md
  - docs/samples.md
  - docs/testing.md
  - _bmad-output/project-context.md
---

# Bondstone BMAD-Native Documentation And V2 Library Reset PRD

## Overview

Bondstone is a .NET library for durable module boundaries, durable command
sending, EF Core backed inbox/outbox persistence, operation observation, and
transport adapter integration. The project is evergreen and has no external
production consumers that must preserve the current documentation model.

The immediate product change is to restart the Bondstone planning system on
the native BMAD Method track. The authoritative planning chain is:

1. this PRD;
2. `_bmad-output/planning-artifacts/architecture.md`;
3. `_bmad-output/planning-artifacts/epics.md`;
4. `_bmad-output/project-context.md` for lean agent implementation rules.

The old non-native planning workflow, decision-file workflow, and duplicated
architecture docs are retired. Consumer-facing docs remain in `docs/`.
Internal durable architecture, planning, and implementation sequencing live in
BMAD planning artifacts.

## Goals

- Make BMAD planning artifacts the source of truth for Bondstone's internal
  product requirements, runtime architecture, and implementation sequence.
- Keep `/docs` focused on package consumers: setup, package discovery,
  packaging, operations, observability, public API review, samples, testing,
  repository workflow, and GitHub work tracking.
- Remove duplicated or obsolete non-native planning and decision-process
  content.
- Preserve the substantive current and v2 design direction from the existing
  architecture docs and plans in BMAD-native artifacts.
- Make future implementation agents route through PRD, Architecture, Epics,
  and project-context rather than retired decision-file review.

## Non-Goals

- Do not implement runtime code changes as part of this documentation reset.
- Do not create or preserve replacement decision-file workflows.
- Do not use non-native planning for this change.
- Do not turn `project-context.md` into a full architecture document.
- Do not add UI, auth, billing, account management, deployment platform, or
  SaaS-product requirements.
- Do not promise compatibility with transitional v1 public APIs beyond the
  compatibility checks explicitly tracked in BMAD epics.

## Stakeholders

- Maintainer: needs one clear planning flow and fewer duplicated instructions.
- Implementation agents: need durable runtime rules, package boundaries, and
  story order without loading scattered retired decision/history files.
- Library consumers: need concise package/user docs that explain setup,
  operations, diagnostics, testing, packaging, and migration guidance.
- Future consumer-project trial: needs a clean v2 library model before using
  Bondstone in a real project.

## Functional Requirements

### FR1 BMAD Source Of Truth

FR1.1 The repository must expose a complete native BMAD planning chain:
`prd.md`, `architecture.md`, `epics.md`, and `project-context.md`.

FR1.2 The PRD must describe product goals, scope, requirements, non-goals,
and success criteria for the current Bondstone v2 reset.

FR1.3 The architecture document must own internal durable runtime rules,
package boundaries, module execution semantics, persistence semantics,
transport semantics, operation observation, diagnostics, and documentation
ownership.

FR1.4 The epics document must break PRD and architecture requirements into
reviewable stories with acceptance criteria and rollback notes.

FR1.5 The readiness report must validate that PRD, architecture, and epics are
discoverable and aligned; UX is explicitly not applicable.

### FR2 Documentation Deduplication

FR2.1 Retired decision files and decision workflow skills must be removed.

FR2.2 Non-native planning artifacts for the documentation reset must be
removed.

FR2.3 Former internal architecture and planning docs must be fully migrated
into BMAD artifacts when their durable content is absorbed.

FR2.4 Root and scoped AGENTS files must route implementation agents to BMAD
planning artifacts and project-context, not retired decision-file review.

FR2.5 README and docs indexes must stop presenting retired decision files or
deleted architecture docs as the durable source of truth.

FR2.6 `/docs` must retain consumer-facing documentation and point to BMAD
architecture for internal design details.

### FR3 Product Model

FR3.1 Bondstone must remain a library/framework for durable module boundaries,
not a general-purpose bus, workflow engine, code generator, or application
framework.

FR3.2 The core value proposition is stable module contracts, stable durable
message identities, local transactional outbox processing, durable receive
inbox processing, operation observation, and service-extraction continuity.

FR3.3 Modular monoliths are the first-class path. Service extraction must be
possible without replacing message contracts, handler patterns, identities,
or inbox/outbox semantics.

FR3.4 Microservice use is supported where consumers need module-owned
durability and host-owned transport infrastructure.

### FR4 Module Execution And Contracts

FR4.1 Modules own module name, durable messaging capability, persistence
binding, command handler registration, published integration event
registration, and event subscriber registration.

FR4.2 Hosts compose modules through `AddBondstone` and module-owned extension
methods.

FR4.3 `IModuleCommandExecutor` is the immediate same-process command boundary;
it is not a generic mediator.

FR4.4 Cross-module state-changing work inside a handler must use durable
commands or integration events unless it is an explicit immediate same-process
module command execution accepted by the architecture.

FR4.5 Query execution should be a separate immediate read boundary that does
not write inbox rows, outbox rows, operation state, or integration events.

### FR5 Durable Messaging

FR5.1 Commands and integration events are distinct durable message kinds.
They must not be collapsed into a generic bus abstraction.

FR5.2 Durable command send accepts work and returns send metadata. It does not
return target handler results directly.

FR5.3 Operation status and results are observed through operation APIs.

FR5.4 Integration events are durable facts with zero or more subscribers.
Fanout is provider-native topology owned by the host.

FR5.5 Domain events are module-local facts. They are not integration events,
transport messages, or automatically published outbox messages.

### FR6 Durable Persistence

FR6.1 Source module state and outgoing durable outbox rows must commit
atomically.

FR6.2 Receive-side module state, receive markers, outgoing rows, operation
state, and domain-event persistence must commit in the owning module
transaction boundary.

FR6.3 EF Core plus PostgreSQL is the supported production durable persistence
path.

FR6.4 Consumers own EF migrations. Bondstone packages do not ship automatic
schema rollout.

FR6.5 The v2 receive model is a single durable receive ledger around durable
inbox ingestion, claim, retry, processed, stale, and terminal failure state.

### FR7 Transport Ownership

FR7.1 Transport adapters are thin native-driver envelope adapters.

FR7.2 Hosts own queues, topics, exchanges, subscriptions, rules, credentials,
prefetch, broker retry, dead-letter policy, workers, and deployment topology.

FR7.3 Bondstone owns durable module semantics: stable identities, module
persistence boundaries, outbox rows, durable inbox rows, command handlers,
event subscriber handlers, and operation finalization semantics.

FR7.4 Native broker delivery must not be acknowledged or completed before
durable inbox ingestion succeeds.

FR7.5 `Bondstone.Transport.Local` is explicit local/dev/test infrastructure,
not production broker durability and not a hidden fallback.

### FR8 Operations And Diagnostics

FR8.1 Bondstone must expose operational evidence for outbox dispatch, durable
receive, operation state, and terminal failures.

FR8.2 Cleanup, retention, replay, purge, stale-row recovery, broker
dead-letter movement, and topology management remain application-owned unless
a future BMAD PRD/architecture explicitly adds them.

FR8.3 Operation observation answers what is known about accepted durable work;
it is not orchestration, saga state, process-manager state, or durable
continuation state.

FR8.4 Diagnostics should remain OpenTelemetry-native where practical and avoid
high-cardinality dimensions such as message ids, operation ids, payloads, and
exception text.

FR8.5 Stable misconfiguration codes are desired for common setup failures.

### FR9 Package And Public API Surface

FR9.1 Package IDs, dependency direction, target framework, versioning, and
publishing policy remain centrally documented in consumer-facing docs.

FR9.2 Public API changes remain compatibility-sensitive and require inventory,
baseline review, and migration notes where applicable.

FR9.3 Production runtime packages must collaborate through explicit contracts
or package-local implementation, not `InternalsVisibleTo`.

FR9.4 Public implementation types exposed temporarily must not be hidden
without classification and review.

### FR10 Testing And Verification

FR10.1 `pnpm check` remains the default quality gate.

FR10.2 Tests use xUnit categories consistently: `Unit`, `Application`,
`Integration`, and `Package`.

FR10.3 EF Core InMemory does not prove relational durability; PostgreSQL,
locking, uniqueness, transactions, claiming, and retries need integration
tests.

FR10.4 Public API baselines guard all packable packages.

FR10.5 Documentation-only changes should run formatting/reference checks; code
or package changes should run the relevant package scripts.

## Nonfunctional Requirements

- NFR1: Planning artifacts must be concise enough for agents to load, but
  complete enough to prevent runtime-design drift.
- NFR2: Documentation cleanup must remove duplicated authority language and
  stale references to retired planning or decision artifacts.
- NFR3: Runtime architecture must preserve stable durable identities and
  persisted-data/message compatibility concerns.
- NFR4: Package/user docs must stay useful to human consumers after internal
  architecture docs move to BMAD.
- NFR5: Story slices must be small, reviewable, and independently verifiable.
- NFR6: The project remains evergreen; deleting obsolete documentation is
  acceptable when its durable content is absorbed.

## UX Requirements

No UX design requirements apply. Bondstone is a library/framework and this
change has no user interface.

## Success Metrics

- Stock BMAD readiness can discover PRD, architecture, and epics under
  `_bmad-output/planning-artifacts/`.
- No repository documentation points agents to retired decision or non-native
  planning artifacts as current workflow.
- Former internal architecture and planning docs are removed when fully
  absorbed.
- Root README and AGENTS files describe BMAD-native source-of-truth routing.
- Project-context is lean, current, and free of retired-source contradictions.
- Remaining `/docs` files are consumer-facing or repository-operation docs,
  not duplicated architecture books.

## Open Items

- Decide during implementation whether `docs/github-workflow.md` keeps a
  BMAD review label or is simplified further.
- Decide whether stable misconfiguration codes are constants, enum values, or
  structured exception properties when that story is implemented.
- Decide the final public API compatibility promise before any stable v2
  package release.
