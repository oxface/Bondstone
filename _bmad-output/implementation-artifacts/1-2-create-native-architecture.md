---
baseline_commit: f9dc177485fc80e496004b0ea44df9fba6365900
---

# Story 1.2: Create Native Architecture

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an implementation agent,
I want architecture under `_bmad-output/planning-artifacts/architecture.md`,
so that internal runtime rules are centralized before implementation.

## Acceptance Criteria

1. Architecture owns internal runtime rules and absorbs the former duplicated architecture and planning content.
2. Architecture covers modules, command/query execution, durable messaging, domain events, persistence, outbox, inbox, transport, hosting, operations, diagnostics, public API, docs ownership, and verification.
3. Architecture does not require retired decision-file review.
4. File exists at `_bmad-output/planning-artifacts/architecture.md`.
5. `bmad-check-implementation-readiness` discovery can match `*architecture*.md`.

## Tasks / Subtasks

- [x] Verify the native architecture artifact exists and is discoverable. (AC: 4, 5)
  - [x] Confirm `_bmad-output/planning-artifacts/architecture.md` exists.
  - [x] Confirm the readiness discovery pattern can match the file with `ls _bmad-output/planning-artifacts/*architecture*.md`.
  - [x] Confirm no competing sharded or duplicate architecture source exists under `_bmad-output/planning-artifacts/`.
- [x] Verify architecture ownership and content coverage. (AC: 1, 2)
  - [x] Confirm the architecture identifies itself as the durable internal architecture source of truth for Bondstone implementation work.
  - [x] Confirm it covers package architecture, module ownership, module command pipeline, module query pipeline, durable commands, integration events, domain events, durable outbox, durable inbox, receive pipeline, operation observation, transport boundary, hosting/workers, persistence, diagnostics, public API, documentation ownership, and verification strategy.
  - [x] Confirm former internal architecture and planning content is represented in BMAD architecture instead of remaining as duplicate `/docs/architecture/**` source-of-truth material.
- [x] Verify retired decision workflow is not required. (AC: 3)
  - [x] Confirm architecture does not instruct agents to create, review, or update retired decision files.
  - [x] Confirm durable requirements, architecture changes, and implementation sequence route through BMAD PRD, architecture, epics, and project-context.
- [x] Preserve rollback boundaries.
  - [x] Limit changes to the architecture artifact unless a broken reference requires a narrow index or routing fix.
  - [x] If durable architecture content was lost, restore only the affected architecture content and references.
- [x] Run documentation-oriented verification.
  - [x] Run targeted reference/content sweeps for architecture ownership and retired decision workflow language.
  - [x] Run formatting or repository checks when available and proportionate for documentation-only changes.

### Review Findings

- [x] [Review][Patch] Dev Agent Record omits the reviewed architecture artifact and claims no architecture content changes [_bmad-output/implementation-artifacts/1-2-create-native-architecture.md:159]
- [x] [Review][Patch] Completion note references an unexplained generated developer guide [_bmad-output/implementation-artifacts/1-2-create-native-architecture.md:154]
- [x] [Review][Patch] Debug log uses stale architecture heading `Public API Strategy` [_bmad-output/implementation-artifacts/1-2-create-native-architecture.md:138]

## Dev Notes

Story 1.2 belongs to Epic 1, "Establish BMAD-Native Planning Authority." It covers PRD FR1.1, FR1.3, and FR2.3: the repository must expose a complete native BMAD planning chain, architecture must own internal durable runtime and package-boundary rules, and former duplicated internal architecture/planning content must be migrated into BMAD artifacts when absorbed.

The current tree already contains `_bmad-output/planning-artifacts/architecture.md`. Treat implementation primarily as verification and narrow correction. Do not create a parallel architecture location, do not move internal runtime architecture into `/docs`, and do not revive retired decision-file review.

### Current Architecture Artifact State

- `_bmad-output/planning-artifacts/architecture.md` has `workflowType: architecture` front matter and `status: final`.
- It states that it is the durable internal architecture source of truth for Bondstone implementation work.
- Its input documents include the PRD, root docs, setup, operations, observability, packaging, public API, and testing docs.
- It is a single whole architecture file, not a sharded architecture workspace.

### Required Architecture Content Signals

- Bondstone is a .NET library for durable module boundaries, not a bus, workflow engine, saga/process-manager framework, broker topology manager, code generator, or SaaS application framework.
- Package responsibilities are explicit for `Bondstone`, `Bondstone.Persistence`, EF Core persistence packages, hosting, local transport, RabbitMQ, and Service Bus.
- Production package collaboration must use public or package-local contracts, not `InternalsVisibleTo`.
- Modules own names, durable messaging capability, persistence binding, command handlers, validators, published events, and subscriber registrations.
- Hosts compose modules through `AddBondstone` and module-owned extensions; provider-native transport setup remains host-owned.
- `IModuleCommandExecutor` is the immediate same-process command boundary, not a generic mediator.
- Durable send accepts work and returns metadata; results are observed through operation APIs.
- Commands, integration events, and domain events remain distinct.
- Source state and outgoing outbox rows commit atomically in the source module transaction boundary.
- Durable inbox owns ingestion, claim, retry, processed, stale, and terminal receive failure state.
- Native broker deliveries must settle only after durable inbox ingestion succeeds.
- EF Core plus PostgreSQL is the supported production durable persistence path; consumers own migrations.
- Diagnostics should be OpenTelemetry-native where practical and avoid high-cardinality dimensions.
- `/docs` owns consumer-facing and repository-operation guidance, while BMAD architecture owns internal durable design.

### Architecture Compliance

Use the BMAD PRD as product scope input and the BMAD epics as sequencing input. The architecture artifact must not become a consumer setup guide or duplicate `/docs` content beyond internal design constraints. Conversely, `/docs` should not become the internal architecture source.

If implementation discovers a missing internal runtime rule, add it to `_bmad-output/planning-artifacts/architecture.md`. If it discovers consumer usage guidance, route it to `/docs` in a later scoped story instead of bloating this artifact.

### File Structure Requirements

Expected files for this story:

- Update or verify `_bmad-output/planning-artifacts/architecture.md`.
- Update this story file only for Dev Agent Record, review findings, and completion status.
- Update `_bmad-output/implementation-artifacts/sprint-status.yaml` through the workflow status transition.

Avoid modifying source packages, tests, samples, scoped READMEs, or consumer docs unless a direct broken architecture reference is found. If a reference fix is required, keep it separately explainable.

### Testing Requirements

This is a documentation/planning artifact story. No runtime test project should be added for Story 1.2.

Recommended verification:

- `ls _bmad-output/planning-artifacts/*architecture*.md`
- `rg -n "Architecture Role|Package Architecture|Module Ownership|Module Command Pipeline|Durable Inbox|Documentation Ownership|Verification Strategy" _bmad-output/planning-artifacts/architecture.md`
- `rg -n "decision-file|docs/adr|docs/architecture|retired" _bmad-output/planning-artifacts/architecture.md README.md AGENTS.md docs _bmad-output`
- `pnpm exec prettier --check _bmad-output/planning-artifacts/architecture.md _bmad-output/implementation-artifacts/1-2-create-native-architecture.md _bmad-output/implementation-artifacts/sprint-status.yaml`
- If proportionate after any broader documentation edits, run `pnpm check`.

### Previous Story Intelligence

Story 1.1 completed and established a useful pattern for this verification slice:

- The PRD workspace index was missing during review even though the story record claimed it existed. For Story 1.2, prove discovery with an actual command before marking AC 5 complete.
- Keep the Dev Agent Record honest. If the architecture artifact is changed, list it in the File List and do not claim verification-only work.
- Use targeted reference sweeps and formatting checks for documentation-only changes.
- Story 1.1 fixed its file list after review; avoid repeating that omission.

### Git Intelligence

Recent commits show this reset is documentation-heavy: `docs: bmad project context`, `chore: bmad`, and inbox-related fixes before the planning reset. The current working tree contains many unrelated documentation/reference changes. Do not revert or normalize unrelated files while completing this story.

### Latest Technical Information

No web research is required for this story. It verifies local BMAD architecture content and discovery paths, not external package or API behavior. Use the locally pinned technology versions from `_bmad-output/project-context.md` and repository configuration if version references need verification.

### Project Context Reference

Project context confirms that BMAD architecture owns runtime architecture and package-boundary rules. It also requires narrow, reviewable edits, no runtime `InternalsVisibleTo` collaboration, explicit durable identities, host-owned transport topology, PostgreSQL-backed proof for relational durability, and repository package scripts for verification.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 1 and Story 1.2
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR1.1, FR1.3, FR2.3
- `_bmad-output/planning-artifacts/architecture.md` - Architecture Role through Explicit Deferred Work
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-18.md` - document discovery and alignment evidence
- `_bmad-output/project-context.md` - source-of-truth routing and implementation rules
- `AGENTS.md` - repository operating rules and architecture routing
- `README.md` - BMAD planning entrypoints
- `docs/README.md` - documentation ownership model

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `ls _bmad-output/planning-artifacts/*architecture*.md`
- `find _bmad-output/planning-artifacts -maxdepth 3 -iname '*architecture*' -print`
- `rg -n "Architecture Role|Package Architecture|Module Ownership|Module Command Pipeline|Module Query Pipeline|Durable Commands|Integration Events|Domain Events|Durable Outbox|Durable Inbox|Receive Pipeline|Operation Observation|Transport Boundary|Hosting And Workers|Persistence Architecture|Diagnostics And Observability|Public API And Compatibility|Documentation Ownership|Verification Strategy" _bmad-output/planning-artifacts/architecture.md`
- `rg -n "create, review, or update retired decision|decision-file review|docs/adr|docs/architecture" _bmad-output/planning-artifacts/architecture.md`
- `rg -n "BMAD planning artifacts|prd.md|architecture.md|epics.md|project-context.md|durable internal architecture source of truth|/docs owns" _bmad-output/planning-artifacts/architecture.md`
- `rg -n "Public API And Compatibility" _bmad-output/planning-artifacts/architecture.md`
- `pnpm exec prettier --check _bmad-output/planning-artifacts/architecture.md _bmad-output/implementation-artifacts/1-2-create-native-architecture.md _bmad-output/implementation-artifacts/sprint-status.yaml`
- `pnpm check`

### Completion Notes List

- Verified the native BMAD architecture artifact exists and is discoverable via the readiness pattern.
- Verified the architecture artifact covers required durable runtime, package-boundary, documentation-ownership, and verification topics.
- Verified no competing `_bmad-output/planning-artifacts/*architecture*` artifact or `docs/architecture` tree exists.
- Verified the architecture artifact does not route agents through retired decision-file review.
- No additional architecture content corrections were required during this verification story; the reviewed architecture artifact remains part of the story record.

### File List

- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/implementation-artifacts/1-2-create-native-architecture.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-06-18: Verified native architecture artifact discovery, content coverage, retired-workflow absence, and documentation quality gates.
