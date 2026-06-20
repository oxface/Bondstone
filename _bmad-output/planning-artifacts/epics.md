---
title: Bondstone BMAD-Native Documentation And V2 Reset Epics
status: final
date: 2026-06-18
workflowType: epics
inputDocuments:
  - _bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md
  - _bmad-output/planning-artifacts/architecture.md
---

# Bondstone BMAD-Native Documentation And V2 Reset Epics

## Overview

These epics convert the PRD and architecture into reviewable implementation
work. The initial focus is documentation source-of-truth cleanup. Runtime code
changes are sequenced after the BMAD planning reset is complete.

Story slices must stay small, reviewable, independently verifiable, and
rollback-aware. Runtime stories use scenario-oriented acceptance criteria for
behavior that affects durable boundaries, transaction semantics, transport
settlement, diagnostics, or public API compatibility.

## FR Coverage Map

| PRD FR | Epic/story coverage                                                 |
| ------ | ------------------------------------------------------------------- |
| FR1.1  | Story 1.1, Story 1.2, Story 1.3, Story 3.5                          |
| FR1.2  | Story 1.1                                                           |
| FR1.3  | Story 1.2                                                           |
| FR1.4  | Story 1.3, every story rollback note, runtime verification criteria |
| FR1.5  | Story 1.4                                                           |
| FR2.1  | Story 2.3, Story 2.4                                                |
| FR2.2  | Story 2.1                                                           |
| FR2.3  | Story 1.2, Story 2.2, Story 3.4                                     |
| FR2.4  | Story 3.2, Story 4.1, Story 4.2                                     |
| FR2.5  | Story 3.1, Story 3.3, Story 4.1, Story 4.2, Story 4.3, Story 8.3    |
| FR2.6  | Story 3.3, Story 3.4                                                |
| FR3.1  | Story 5.1                                                           |
| FR3.2  | Story 5.1, Story 5.4, Story 6.1, Story 6.2, Story 7.2               |
| FR3.3  | Story 5.1, Story 5.2, Story 5.3, Story 7.1                          |
| FR3.4  | Story 5.1, Story 7.1                                                |
| FR4.1  | Story 5.2                                                           |
| FR4.2  | Story 5.2                                                           |
| FR4.3  | Story 5.3                                                           |
| FR4.4  | Story 5.3, Story 5.4                                                |
| FR4.5  | Story 5.3                                                           |
| FR5.1  | Story 5.4                                                           |
| FR5.2  | Story 5.3                                                           |
| FR5.3  | Story 7.2                                                           |
| FR5.4  | Story 7.1                                                           |
| FR5.5  | Story 5.4                                                           |
| FR6.1  | Story 6.1                                                           |
| FR6.2  | Story 6.2                                                           |
| FR6.3  | Story 6.3, Story 8.5                                                |
| FR6.4  | Story 6.3                                                           |
| FR6.5  | Story 6.2                                                           |
| FR7.1  | Story 7.1                                                           |
| FR7.2  | Story 7.1                                                           |
| FR7.3  | Story 7.1                                                           |
| FR7.4  | Story 6.2, Story 7.1                                                |
| FR7.5  | Story 7.1                                                           |
| FR8.1  | Story 6.1, Story 6.2, Story 7.2, Story 7.3                          |
| FR8.2  | Story 7.3                                                           |
| FR8.3  | Story 7.2                                                           |
| FR8.4  | Story 7.4                                                           |
| FR8.5  | Story 7.4                                                           |
| FR9.1  | Story 8.2                                                           |
| FR9.2  | Story 8.1                                                           |
| FR9.3  | Story 8.1                                                           |
| FR9.4  | Story 8.1                                                           |
| FR10.1 | Story 8.4                                                           |
| FR10.2 | Story 8.4                                                           |
| FR10.3 | Story 6.2, Story 6.3                                                |
| FR10.4 | Story 8.1, Story 8.4                                                |
| FR10.5 | Story 8.4                                                           |

## Epic 1: Establish BMAD-Native Planning Authority

Goal: Maintain native BMAD artifacts that stock BMAD workflows can discover.

### Story 1.1: Create Native PRD

Covers: FR1.1, FR1.2

As a maintainer,
I want a PRD workspace under `_bmad-output/planning-artifacts/prds/`,
So that requirements are discoverable by BMAD planning and readiness flows.

Acceptance Criteria:

- PRD describes Bondstone product scope, goals, non-goals, requirements, and
  success criteria.
- PRD names BMAD artifacts as the planning chain.
- PRD explicitly marks UX as not applicable.
- PRD does not preserve retired non-native planning workflow requirements.

Verification:

- File exists at
  `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md`.
- PRD workspace index exists at `_bmad-output/planning-artifacts/prds/index.md`.
- `bmad-check-implementation-readiness` discovery can match the sharded PRD
  through `_bmad-output/planning-artifacts/prds/index.md`.

Rollback:

- Revert only the PRD workspace files if discovery or source routing regresses.

### Story 1.2: Create Native Architecture

Covers: FR1.1, FR1.3, FR2.3

As an implementation agent,
I want architecture under `_bmad-output/planning-artifacts/architecture.md`,
So that internal runtime rules are centralized before implementation.

Acceptance Criteria:

- Architecture owns internal runtime rules and absorbs the former duplicated
  architecture and planning content.
- Architecture covers modules, command/query execution, durable messaging,
  domain events, persistence, outbox, inbox, transport, hosting, operations,
  diagnostics, public API, docs ownership, and verification.
- Architecture does not require retired decision-file review.

Verification:

- File exists at `_bmad-output/planning-artifacts/architecture.md`.
- `bmad-check-implementation-readiness` discovery can match
  `*architecture*.md`.

Rollback:

- Revert the architecture artifact update and restore any affected references
  only if durable architecture content was lost.

### Story 1.3: Create Native Epics

Covers: FR1.1, FR1.4

As a developer agent,
I want implementation work sequenced in `_bmad-output/planning-artifacts/epics.md`,
So that sprint planning can proceed without reading obsolete plans.

Acceptance Criteria:

- Epics are derived from PRD and architecture.
- Stories are reviewable and include acceptance criteria.
- Stories include verification guidance and rollback notes.
- Documentation cleanup precedes runtime implementation work.
- FR traceability is maintained in either the coverage map or story metadata.

Verification:

- File exists at `_bmad-output/planning-artifacts/epics.md`.
- `bmad-check-implementation-readiness` discovery can match `*epic*.md`.
- The FR Coverage Map accounts for every PRD functional requirement.

Rollback:

- Revert the epics update if the artifact stops matching the PRD or
  architecture source of truth.

### Story 1.4: Create Readiness Report

Covers: FR1.5

As a maintainer,
I want a readiness report for the new artifact set,
So that implementation starts from an aligned source of truth.

Acceptance Criteria:

- Report validates PRD, architecture, and epics alignment.
- UX is marked not applicable.
- Concerns and follow-ups are explicit.

Verification:

- Readiness output names included PRD, architecture, and epics artifacts.
- Any missing or partial FR coverage is either fixed in `epics.md` or tracked
  as a deliberate deferred item.

Rollback:

- Delete or regenerate the readiness report if it describes an artifact set
  that is no longer current.

## Epic 2: Keep Retired Planning Workflows Removed

Goal: Keep obsolete artifacts and skills from reappearing after the reset.

### Story 2.1: Guard Against Non-Native Planning Artifacts

Covers: FR2.2

As a maintainer,
I want retired non-native planning artifacts to stay absent,
So that agents treat the PRD as the requirements source.

Acceptance Criteria:

- Active docs do not name non-native planning artifacts as current
  requirements.
- New planning work starts from native BMAD PRD, architecture, epics, or the
  appropriate BMAD workflow.
- Generated artifacts are clearly marked as generated if they are not source
  of truth.

Verification:

- Reference sweeps find no current routing to retired non-native planning
  artifacts.

Rollback:

- Recover old material from git history only if a missed requirement is found.

### Story 2.2: Guard Against Duplicate Architecture Artifacts

Covers: FR2.3

As a maintainer,
I want duplicate internal architecture artifacts to stay absent,
So that implementation agents use one architecture source.

Acceptance Criteria:

- Active docs point to `_bmad-output/planning-artifacts/architecture.md`.
- New architecture material is added to the BMAD architecture artifact or a
  future BMAD-native architecture workflow output, not to consumer docs.

Verification:

- Reference sweeps find no current source-of-truth routing to deleted
  `docs/architecture/**` files.

Rollback:

- Restore a duplicate architecture artifact only if a missed internal rule
  cannot be represented in the BMAD architecture artifact.

### Story 2.3: Guard Against Retired Decision Files

Covers: FR2.1

As a maintainer,
I want retired decision files to stay absent,
So that durable decisions route through BMAD artifacts.

Acceptance Criteria:

- Root and scoped docs do not link to retired decision files.
- No active instruction says broad changes require retired decision-file
  review.
- Durable requirements, architecture, and sequence changes update BMAD
  artifacts.

Verification:

- Reference sweeps find no active links to `docs/adr/**` as current workflow.

Rollback:

- Recover old decision material from git history only for archaeology or
  missed requirement recovery.

### Story 2.4: Guard Against Retired Decision Skills

Covers: FR2.1

As an agent maintainer,
I want retired decision workflow skills to stay absent,
So that agents cannot accidentally start the old workflow.

Acceptance Criteria:

- `.agents/skills/AGENTS.md` describes BMAD artifact ownership.
- No repository-local skill description routes durable decisions through the
  retired workflow.

Verification:

- Skill index and local skill directories contain no active ADR workflow
  routing.

Rollback:

- Restore retired skills only if the project deliberately returns to the old
  decision workflow through a future PRD and architecture update.

## Epic 3: Deduplicate Repository And Consumer Documentation

Goal: Keep `/docs` useful for consumers while moving internal durable
architecture into BMAD artifacts.

### Story 3.1: Rewrite Root README

Covers: FR2.5

As a repository visitor,
I want README to explain package purpose and BMAD-native source routing,
So that I can find setup, verification, and planning artifacts quickly.

Acceptance Criteria:

- README links to PRD, architecture, epics, and project-context.
- README links to consumer docs that remain under `/docs`.
- README does not link to retired decision or deleted architecture docs.

Verification:

- README links resolve to existing files.

Rollback:

- Revert README routing only if a link points to the wrong current authority.

### Story 3.2: Rewrite Root AGENTS

Covers: FR2.4

As an agent,
I want root `AGENTS.md` to route me to BMAD artifacts and scoped docs,
So that I load the right source before editing.

Acceptance Criteria:

- Root AGENTS points to PRD, architecture, epics, and project-context.
- Root AGENTS keeps operating rules and verification commands.
- Root AGENTS removes retired decision workflow requirements and non-native
  planning routing.

Verification:

- Root AGENTS links resolve to existing files.

Rollback:

- Revert only the root AGENTS routing if agents are sent to wrong or missing
  files.

### Story 3.3: Rewrite Docs Indexes

Covers: FR2.5, FR2.6

As a docs maintainer,
I want `docs/README.md` and `docs/AGENTS.md` to describe consumer-doc
ownership,
So that `/docs` no longer competes with BMAD architecture.

Acceptance Criteria:

- `docs/README.md` lists remaining consumer/repository docs.
- `docs/AGENTS.md` points architecture work to BMAD architecture.
- Removed docs are not linked.

Verification:

- Docs index links resolve to existing files.

Rollback:

- Revert docs index changes only if consumer docs become harder to discover.

### Story 3.4: Keep Internal Architecture Out Of Consumer Docs

Covers: FR2.3, FR2.6

As a maintainer,
I want internal architecture to stay in BMAD artifacts,
So that there is one source for internal architecture.

Acceptance Criteria:

- Consumer docs link to BMAD architecture instead of duplicating internal
  design.
- Package README, scoped AGENTS, and docs do not link to deleted paths.
- New internal architecture sections are added to BMAD architecture.

Verification:

- Reference sweeps show consumer docs as usage/operation guidance rather than
  duplicated architecture books.

Rollback:

- Restore consumer-doc detail only for usage guidance that does not belong in
  architecture.

### Story 3.5: Rewrite Project Context

Covers: FR1.1

As an implementation agent,
I want `_bmad-output/project-context.md` to be lean and current,
So that it guides work without duplicating architecture.

Acceptance Criteria:

- Project context removes retired-source contradictions.
- Project context links to BMAD PRD, architecture, and epics.
- Project context keeps critical technology, code, testing, and workflow rules.

Verification:

- Project context links resolve and remain concise enough for agent loading.

Rollback:

- Restore omitted agent guardrails only by adding concise references or rules,
  not by reintroducing full architecture duplication.

## Epic 4: Align Package And Scoped Agent References

Goal: Remove stale links from scoped READMEs and AGENTS files after deleting
architecture and retired decision docs.

### Story 4.1: Update Source Package References

Covers: FR2.4, FR2.5

As a package maintainer,
I want source package README and AGENTS references to point to current docs,
So that agents and humans are not sent to deleted paths.

Acceptance Criteria:

- `src/**/AGENTS.md` links to BMAD architecture or consumer docs.
- `src/**/README.md` links do not point to removed architecture files.
- Package-specific consumer docs remain linked.

Verification:

- `rg` reference sweeps over `src/**` find no active deleted-path links.

Rollback:

- Revert a scoped reference only if it incorrectly points away from the
  package's current owning document.

### Story 4.2: Update Test References

Covers: FR2.4, FR2.5

As a test maintainer,
I want test AGENTS/README files to reference current verification and
architecture sources,
So that test changes use the right constraints.

Acceptance Criteria:

- `tests/**/AGENTS.md` avoids retired decision links.
- Persistence/transport tests point to BMAD architecture plus relevant
  consumer docs.
- Public API test docs no longer say retired decision review is required.

Verification:

- `rg` reference sweeps over `tests/**` find no active deleted-path links.

Rollback:

- Revert a scoped test reference only if the current verification owner is
  wrong.

### Story 4.3: Update GitHub Workflow Guidance

Covers: FR2.5

As a maintainer,
I want GitHub work tracking to fit BMAD-native decisions,
So that issues remain useful without retired decision labels driving
architecture.

Acceptance Criteria:

- `docs/github-workflow.md` uses BMAD review semantics.
- Issue guidance points durable decisions to BMAD PRD/architecture/epics.
- Backlog tracking remains in GitHub Issues/Projects.

Verification:

- GitHub workflow guidance contains no current requirement for retired ADR
  labels or decision-file review.

Rollback:

- Restore old label guidance only if a future BMAD workflow deliberately
  reintroduces it.

## Epic 5: Runtime Module Boundaries And Message Contracts

Goal: Preserve Bondstone's product model and module contract semantics before
runtime implementation expands.

### Story 5.1: Product Boundary And Extraction Guardrail

Covers: FR3.1, FR3.2, FR3.3, FR3.4

As a maintainer,
I want runtime implementation stories to preserve Bondstone's library boundary,
So that v2 work does not drift into a bus, workflow engine, code generator, or
application framework.

Acceptance Criteria:

- Given a runtime story changes messaging, hosting, or package APIs, when it is
  reviewed, then it names how the change preserves durable module boundaries
  rather than generic bus or workflow scope.
- Given a module moves from in-process composition toward service extraction,
  when contracts are reused, then stable message identities, handler patterns,
  inbox/outbox semantics, and operation observation remain valid.
- Given a microservice host uses Bondstone, when broker infrastructure is
  configured, then durability remains module-owned and transport topology
  remains host-owned.
- Given docs describe product scope, when they mention unsupported categories,
  then they keep bus, workflow, generator, SaaS, and app-platform scope out of
  Bondstone's current promise.

Verification:

- Architecture, package docs, and affected tests preserve the product boundary
  and service-extraction language.

Rollback:

- Revert any product-scope expansion that lacks PRD and architecture approval.

### Story 5.2: Module Registration And Host Composition

Covers: FR4.1, FR4.2, FR3.3

As a host developer,
I want modules to own their registration metadata while hosts compose them,
So that module boundaries stay explicit and extraction-friendly.

Acceptance Criteria:

- Given a module is registered, when durable messaging is enabled, then module
  name, persistence binding, command handlers, validators, published events,
  and subscriber handlers are declared by the module.
- Given a host composes an application, when it calls `AddBondstone`, then it
  can compose module-owned extension methods with host-owned environment
  inputs.
- Given duplicate or conflicting module durable registrations exist, when
  startup validation runs, then it fails with a stable diagnostic shape.
- Given a module registers only remote durable message identities, when no
  local route exists, then registration does not imply a local handler.

Verification:

- Unit or application tests cover successful module composition, duplicate
  registration failure, and remote-contract-only identity registration.

Rollback:

- Revert composition API changes if they obscure module ownership or require
  hosts to declare module-owned metadata directly.

### Story 5.3: Immediate Command And Query Boundaries

Covers: FR4.3, FR4.4, FR4.5, FR5.2

As a consumer,
I want immediate commands and queries to have explicit semantics,
So that callers do not confuse local execution, durable send, and read-only
module access.

Acceptance Criteria:

- Given `IModuleCommandExecutor` executes a command, when the handler runs,
  then it uses Bondstone's module command pipeline and is not exposed as a
  generic mediator.
- Given cross-module state-changing work must survive restart or extraction,
  when it is invoked from a handler, then durable commands or integration
  events are used unless immediate same-process execution is explicitly
  accepted.
- Given a durable command is sent, when the sender completes, then it returns
  accepted-work metadata rather than the target handler result.
- Given a module query executes, when it completes, then it writes no inbox,
  outbox, operation, domain-event, or integration-event state.
- Given a command or query route is missing, when execution is attempted, then
  diagnostics identify the missing route without deriving durable identities
  from CLR names.

Verification:

- Unit or application tests cover command execution, durable-send metadata,
  query no-write behavior, and missing route diagnostics.

Rollback:

- Revert command/query API changes if immediate and durable semantics become
  ambiguous.

### Story 5.4: Durable Message Kind And Domain Event Boundaries

Covers: FR5.1, FR5.5, FR4.4, FR3.2

As an application developer,
I want commands, integration events, and domain events to stay distinct,
So that durable behavior remains explicit and restart-safe.

Acceptance Criteria:

- Given a durable command is sent, when it is serialized, then it carries a
  command identity and target module rather than an event fanout contract.
- Given an integration event is published, when it is dispatched, then it has
  no single target module and can fan out to zero or more stable subscribers.
- Given a domain event is raised, when EF-backed domain-event persistence is
  enabled, then it remains module-local and is not automatically staged as an
  outbox integration event.
- Given a domain event should become public, when integration publication is
  required, then explicit module code maps it to an integration event.
- Given a story or API proposes a generic bus abstraction, when reviewed, then
  it is rejected unless PRD and architecture are updated first.

Verification:

- Tests cover command/event envelope distinction, subscriber identity, and
  absence of automatic domain-to-integration publication.

Rollback:

- Revert any generic-message abstraction that collapses command, integration
  event, or domain-event semantics.

## Epic 6: Durable Persistence And Receive Ledger

Goal: Prove durable persistence behavior through narrow transaction and
provider-backed slices.

### Story 6.1: Source Outbox Atomicity

Covers: FR6.1, FR8.1, FR3.2

As a module owner,
I want source state and outgoing durable envelopes to commit atomically,
So that accepted durable work is not separated from the state change that
caused it.

Acceptance Criteria:

- Given a handler changes source module state and sends or publishes durable
  work, when the transaction commits, then state and outbox rows are both
  visible.
- Given the transaction rolls back, when state is inspected, then no outgoing
  outbox row remains for the failed work.
- Given an outbox dispatch fails terminally, when operations are inspected,
  then terminal outbox evidence is visible without relying on broker DLQ
  ownership.
- Given concurrency or uniqueness behavior matters, when tests are written,
  then they use PostgreSQL-backed integration tests rather than EF InMemory.

Verification:

- PostgreSQL integration tests cover commit, rollback, duplicate, and terminal
  outbox evidence paths.

Rollback:

- Revert persistence changes if source state and outbox rows can diverge.

### Story 6.2: Durable Receive Transaction Boundary

Covers: FR6.2, FR6.5, FR7.4, FR10.3, FR8.1

As an operator,
I want one durable receive ledger,
So that ingestion, retry, terminal failure, stale state, and processed state
are visible in one place.

Acceptance Criteria:

- Given a native broker delivery arrives, when the envelope is valid, then it
  is durably ingested before native settlement.
- Given ingestion fails, when the adapter handles the delivery, then native
  broker settlement does not acknowledge or complete the delivery as processed.
- Given receive processing succeeds, when the module transaction commits, then
  receive markers, module state, outgoing rows, operation state, domain-event
  records, and durable inbox state commit in the owning boundary where
  applicable.
- Given receive processing fails, when retry policy allows more attempts, then
  the durable inbox row records retry state.
- Given retry is exhausted or failure is terminal, when inspected, then the
  durable inbox row exposes terminal evidence.
- Given the transitional direct `inbox_messages` marker remains, when runtime
  work proceeds, then it is hidden as an implementation detail and not the
  operator-facing ledger.

Verification:

- PostgreSQL and adapter-appropriate tests cover duplicate ingestion, retry,
  terminal failure, processed state, stale state, and settlement timing.

Rollback:

- Revert receive-ledger changes if durable ingestion no longer precedes native
  settlement or receive outcomes split across competing operator ledgers.

### Story 6.3: EF/PostgreSQL Production Persistence And Migrations

Covers: FR6.3, FR6.4, FR10.3

As a library consumer,
I want EF Core plus PostgreSQL to be the supported production durability path,
So that persistence behavior and migration ownership are clear.

Acceptance Criteria:

- Given production durable persistence is documented, when consumers read setup
  and operations guidance, then EF Core plus PostgreSQL is named as the
  supported production path.
- Given schema changes are required, when package guidance is reviewed, then
  consumers own EF migrations and Bondstone does not ship automatic schema
  rollout.
- Given provider-specific semantics matter, when tests cover persistence,
  then PostgreSQL integration tests prove uniqueness, transactions, locking,
  claiming, retry, and terminal state where applicable.
- Given EF InMemory tests exist, when reviewed, then they are limited to fast
  mapping or change-tracker boundaries and not used as relational proof.

Verification:

- Docs and integration tests make EF/PostgreSQL support and migration
  ownership explicit.

Rollback:

- Revert docs or code that imply Bondstone owns consumer schema rollout.

## Epic 7: Transport, Operations, And Diagnostics

Goal: Keep broker ownership host-owned while exposing Bondstone-owned durable
evidence and low-cardinality diagnostics.

Epic 7 is runtime-first. Each story starts with an implementation inventory,
but it may close as documentation-only only when the story records evidence
that current code and tests already satisfy the acceptance criteria. Otherwise
the story must add or adjust runtime code, tests, or public inspection
surfaces that make the durable evidence observable.

### Story 7.1: Thin Transport And Fanout Ergonomics

Covers: FR5.4, FR7.1, FR7.2, FR7.3, FR7.4, FR7.5, FR3.4

As a host developer,
I want module-aware transport bindings without Bondstone owning topology,
So that native broker infrastructure remains app-owned.

Acceptance Criteria:

- Given a transport adapter sends or receives, when it crosses the boundary,
  then it only translates native messages and Bondstone durable envelopes.
- Given command dispatch is configured, when one durable envelope is routed,
  then it maps to exactly one outbound route.
- Given integration event dispatch is configured, when subscribers exist, then
  fanout uses provider-native topology owned by the host.
- Given queues, topics, exchanges, subscriptions, rules, credentials, retry,
  dead-letter policy, workers, or deployment topology are needed, when setup is
  reviewed, then the host owns them.
- Given Bondstone durable semantics are inspected, when transport is involved,
  then Bondstone owns stable identities, module persistence boundaries, outbox
  rows, durable inbox rows, command handlers, subscriber handlers, and
  operation finalization.
- Given `Bondstone.Transport.Local` is configured, when production guidance is
  reviewed, then it is explicit local/dev/test infrastructure and never a
  hidden fallback for missing broker configuration.
- Given native deliveries are received, when durable ingestion fails, then the
  adapter does not acknowledge or complete the native delivery as processed.

Verification:

- Unit or application tests cover ambiguous routes, missing bindings, local
  transport opt-in behavior, and thin adapter boundaries. Provider-backed
  integration tests cover native settlement where needed.

Rollback:

- Revert transport changes that move topology, retry, DLQ, or production
  fallback ownership into Bondstone.

### Story 7.2: Operation Observation Cleanup

Covers: FR5.3, FR8.1, FR8.3, FR3.2

As an operator,
I want operation APIs framed as observation,
So that waits/results are not confused with orchestration.

Acceptance Criteria:

- Given durable work is accepted, when a caller receives metadata, then
  operation status and result are observed through operation APIs.
- Given wait helpers are used, when a caller times out, then timeout does not
  write operation state or create orchestration semantics.
- Given operation APIs are documented, when reviewed, then they do not imply
  saga, workflow, process-manager, or durable continuation state.
- Given terminal outbox, receive, operation, or worker failures occur, when
  operators inspect state, then evidence is discoverable through documented
  surfaces.
- Given accepted durable work completes, fails, or remains pending, when
  operation APIs are exercised in tests, then status reads, typed result reads,
  and timeout behavior are proven at the API boundary rather than only
  described in docs.
- Given inventory finds that terminal outbox, receive, operation, or worker
  evidence is only available through ad hoc persistence queries, when the story
  closes, then it either adds an explicit non-orchestrating observation surface
  or records why the existing public API is sufficient.

Verification:

- Tests and docs cover accepted metadata, status reads, typed result reads,
  short waits, timeout behavior, and operation troubleshooting.
- The story includes runtime/test changes unless the inventory records that
  current APIs and tests already cover every operation-observation criterion.

Rollback:

- Revert operation API wording or helper changes that imply orchestration.

### Story 7.3: App-Owned Worker Operations And Retention

Covers: FR8.2, FR8.1

As an operator,
I want explicit inspection and cleanup guidance,
So that durable evidence is not destroyed by defaults.

Acceptance Criteria:

- Given cleanup, retention, replay, purge, stale-row recovery, broker
  dead-letter movement, or topology management is needed, when documentation is
  reviewed, then it is application-owned unless a future BMAD PRD and
  architecture add native support.
- Given worker health guidance is reviewed, when terminal outbox failures,
  stale inbox rows, terminal receive failures, worker failures, or operation
  expiration backlog exist, then inspection recipes describe the evidence.
- Given helper APIs are added, when reviewed, then they are explicit and opt in
  rather than automatic destructive defaults.
- Given inspection recipes depend on durable evidence, when feasible, then
  tests exercise the evidence source or helper API so the story does not close
  on prose alone.
- Given a cleanup, replay, purge, stale-row, or DLQ action would be destructive
  or provider-owned, when no helper is added, then the story records that as a
  deliberate non-feature and leaves only non-destructive inspection support.

Verification:

- Operations docs and tests, where applicable, cover inspection without adding
  default cleanup workers.
- The story includes at least one non-documentation verification artifact for
  worker/evidence inspection, or records why the existing tests already prove
  the required inspection behavior.

Rollback:

- Revert default cleanup, replay, purge, or DLQ movement behavior that lacks
  future PRD and architecture approval.

### Story 7.4: OpenTelemetry Diagnostics And Stable Setup Codes

Covers: FR8.4, FR8.5

As a consumer,
I want stable setup failure codes and low-cardinality diagnostics,
So that common misconfigurations can be detected and documented consistently.

Acceptance Criteria:

- Given diagnostics are emitted, when OpenTelemetry-native activity, metric,
  or log surfaces are practical, then they are used.
- Given diagnostics include dimensions, when reviewed, then message ids,
  operation ids, exception text, payloads, broker delivery counts, topology
  details, and dead-letter state are not used as high-cardinality dimensions.
- Given common setup failures occur, when exceptions or validation results are
  produced, then stable codes cover missing module persistence, missing EF
  mappings, missing dispatcher, duplicate durable registrations, invalid
  durable identities, missing receive binding, and ambiguous dispatch routes.
- Given stable codes are added, when compatibility is reviewed, then the code
  representation and migration promise are documented.
- Given common setup failures are already thrown today, when this story is
  implemented, then representative failures emit stable code values through a
  source-compatible exception or validation surface instead of relying only on
  message text.
- Given diagnostics are added or changed, when tests inspect emitted data, then
  they assert stable low-cardinality names and dimensions without matching
  volatile ids, payloads, or exception text.

Verification:

- Unit or application tests cover code emission and low-cardinality diagnostic
  dimensions without asserting volatile payload values.
- This story is not documentation-only unless inventory proves stable setup
  codes and diagnostics already exist with test coverage for the listed common
  failures.

Rollback:

- Revert diagnostic changes that expose high-cardinality dimensions or
  unstable setup-code contracts.

## Epic 8: Public API, Verification, And Trial Readiness

Goal: Prove the BMAD-native docs and v2 reset are ready for consumer-project
trial.

### Story 8.1: Public API And Package Collaboration Validation

Covers: FR9.2, FR9.3, FR9.4, FR10.4

As a maintainer,
I want v2 public API reviewed before release,
So that package consumers get deliberate compatibility posture.

Acceptance Criteria:

- Given public or protected APIs change, when baselines are reviewed, then
  affected APIs are classified as normal setup APIs, documented advanced
  composition APIs, temporarily exposed public implementation types, or
  accidental/obsolete surface.
- Given runtime packages need to collaborate, when implementation is reviewed,
  then production package collaboration uses explicit contracts or
  package-local implementation, not `InternalsVisibleTo`.
- Given temporary public implementation types are hidden or renamed, when the
  change is reviewed, then replacement contracts and migration notes exist.
- Given packable package baselines change, when tests run, then public API
  baselines guard all packable packages.

Verification:

- Public API baseline tests and migration notes are updated only after
  compatibility review.

Rollback:

- Revert public API changes that lack classification, replacement contracts,
  or migration notes.

### Story 8.2: Package Policy And Discovery Documentation

Covers: FR9.1

As a library consumer,
I want package IDs, dependency direction, target framework, versioning, and
publishing policy documented centrally,
So that setup and upgrade choices are discoverable.

Acceptance Criteria:

- Given package guidance is reviewed, when package IDs or namespaces are
  needed, then `docs/packaging.md` and `docs/package-discovery.md` are the
  central consumer-facing references.
- Given dependency direction or target framework changes, when docs are
  updated, then package policy stays centralized instead of scattered through
  scoped READMEs.
- Given publishing or versioning policy changes, when release work proceeds,
  then migration and package docs are updated together.

Verification:

- Package docs link from README/docs index and scoped READMEs without stale
  package IDs.

Rollback:

- Revert scattered package policy text in favor of central docs.

### Story 8.3: Documentation Reference Sweep

Covers: FR2.5

As a maintainer,
I want stale references removed,
So that deleted decision, planning, architecture, and plan paths do not remain.

Acceptance Criteria:

- `rg` finds no active links to deleted paths.
- Remaining retired-workflow mentions are historical only if intentionally
  retained.
- README, AGENTS, docs index, package READMEs, and test AGENTS are aligned.

Verification:

- Run reference sweeps for deleted `docs/adr/**`, `docs/architecture/**`, and
  `docs/plans/**` paths.

Rollback:

- Restore a removed reference only if the target file is restored as a current
  source.

### Story 8.4: Verification Gate

Covers: FR10.1, FR10.2, FR10.4, FR10.5

As a maintainer,
I want the repo verification surface green,
So that the reset is reviewable.

Acceptance Criteria:

- Given documentation-only changes are made, when verification runs, then
  formatting and reference checks run where available.
- Given runtime code or package changes occur, when verification runs, then
  targeted tests run first and the relevant package scripts run afterward.
- Given the default quality gate is named, when repo instructions are reviewed,
  then `pnpm check` is the default gate.
- Given tests are added or reclassified, when test traits are reviewed, then
  xUnit categories are consistently one of `Unit`, `Application`,
  `Integration`, or `Package`.
- Given packable packages change, when verification runs, then public API
  baseline and package checks are included.

Verification:

- `pnpm check` is the default gate, with `pnpm backend:test:integration` used
  for provider-backed behavior and `pnpm backend:pack` used for package
  artifact checks.

Rollback:

- Revert verification-routing changes that make the default gate ambiguous or
  weaken category consistency.

### Story 8.5: Consumer Trial Handoff

Covers: FR6.3, FR3.3

As a maintainer,
I want a clear first-consumer migration handoff,
So that real feedback can drive the next library iteration.

Acceptance Criteria:

- Setup docs identify the recommended EF/PostgreSQL path.
- Package discovery points to current package IDs.
- BMAD epics identify what remains before trial.
- GitHub Issues/Projects track deferred work.
- Service-extraction proof requirements are visible before trial scope is
  treated as complete.

Verification:

- Consumer trial guidance links to setup, package discovery, testing,
  operations, and BMAD epics.

Rollback:

- Revert trial-readiness claims if required v2 runtime stories remain
  incomplete or untracked.
