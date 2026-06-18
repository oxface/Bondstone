---
baseline_commit: f9dc177485fc80e496004b0ea44df9fba6365900
---

# Story 1.3: Create Native Epics

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a developer agent,
I want implementation work sequenced in `_bmad-output/planning-artifacts/epics.md`,
so that sprint planning can proceed without reading obsolete plans.

## Acceptance Criteria

1. Epics are derived from PRD and architecture.
2. Stories are reviewable and include acceptance criteria.
3. Stories include verification guidance and rollback notes.
4. Documentation cleanup precedes runtime implementation work.
5. FR traceability is maintained in either the coverage map or story metadata.
6. File exists at `_bmad-output/planning-artifacts/epics.md`.
7. `bmad-check-implementation-readiness` discovery can match `*epic*.md`.
8. The FR Coverage Map accounts for every PRD functional requirement.

## Tasks / Subtasks

- [x] Verify the native epics artifact exists and is discoverable. (AC: 6, 7)
  - [x] Confirm `_bmad-output/planning-artifacts/epics.md` exists.
  - [x] Confirm readiness discovery can match the file with `ls _bmad-output/planning-artifacts/*epic*.md`.
  - [x] Confirm no competing sharded or duplicate epics source exists under `_bmad-output/planning-artifacts/`.
- [x] Verify PRD and architecture derivation. (AC: 1)
  - [x] Confirm `epics.md` front matter lists the PRD and architecture as input documents.
  - [x] Confirm Epic 1 establishes BMAD-native planning authority before later cleanup and runtime work.
  - [x] Confirm runtime stories preserve product boundaries, durable message semantics, persistence semantics, transport ownership, operation observation, diagnostics, public API, and verification rules from architecture.
- [x] Verify story quality and sequencing. (AC: 2, 3, 4)
  - [x] Confirm every story has a user-value statement, acceptance criteria, verification guidance, and rollback guidance.
  - [x] Confirm documentation cleanup epics precede runtime implementation epics.
  - [x] Confirm high-risk runtime stories use scenario-oriented acceptance criteria where behavior affects durable boundaries, transactions, transport settlement, diagnostics, or public API compatibility.
- [x] Verify FR traceability. (AC: 5, 8)
  - [x] Confirm the FR Coverage Map includes every numbered PRD FR from FR1.1 through FR10.5.
  - [x] Confirm each Epic 1 story includes `Covers:` metadata for its PRD FRs.
  - [x] Confirm no coverage entry points to a missing story key.
- [x] Preserve rollback boundaries.
  - [x] Revert only the epics artifact update if it stops matching PRD or architecture source-of-truth content.
  - [x] Do not modify runtime code or consumer docs unless a direct broken epics reference is found.
- [x] Run documentation-oriented verification.
  - [x] Run targeted FR coverage and discovery checks.
  - [x] Run formatting or repository checks when available and proportionate for documentation-only changes.

### Review Findings

- [x] [Review][Patch] Dev Agent Record omits the reviewed epics artifact and claims no epics content changes [_bmad-output/implementation-artifacts/1-3-create-native-epics.md:166]
- [x] [Review][Patch] Completion note references an unexplained generated developer guide [_bmad-output/implementation-artifacts/1-3-create-native-epics.md:159]

## Dev Notes

Story 1.3 belongs to Epic 1, "Establish BMAD-Native Planning Authority." It covers PRD FR1.1 and FR1.4: the repository must expose a complete BMAD planning chain, and the epics document must translate PRD plus architecture into reviewable implementation stories with acceptance criteria and rollback notes.

The current tree already contains `_bmad-output/planning-artifacts/epics.md`. Treat implementation primarily as verification and narrow correction. Do not create a parallel plan, do not revive obsolete planning documents, and do not change runtime scope from this story.

### Current Epics Artifact State

- `_bmad-output/planning-artifacts/epics.md` has `workflowType: epics` front matter and `status: final`.
- It lists the PRD and architecture as input documents.
- It contains an FR Coverage Map from FR1.1 through FR10.5.
- It defines Epic 1 through Epic 8 and 32 implementation stories.
- Documentation source-of-truth cleanup appears before runtime implementation work.

### Required Epics Content Signals

- Epic 1 establishes native BMAD planning authority.
- Epic 2 keeps retired planning workflows removed.
- Epic 3 deduplicates repository and consumer documentation.
- Epic 4 aligns package and scoped agent references.
- Epic 5 protects runtime module boundaries and message contracts.
- Epic 6 proves durable persistence and receive ledger behavior.
- Epic 7 covers transport, operations, and diagnostics while keeping broker topology host-owned.
- Epic 8 validates public API, package policy, verification, and consumer-trial readiness.
- Story slices must stay small, reviewable, independently verifiable, and rollback-aware.
- Runtime behavior stories should be scenario-oriented where durable semantics or compatibility are at risk.

### Architecture Compliance

Use `_bmad-output/planning-artifacts/architecture.md` as the internal design source. The epics file should sequence architecture work, not redefine architecture in competing language. Runtime stories must preserve the architecture's distinctions between immediate commands, durable commands, integration events, domain events, outbox, inbox, transport boundaries, and operation observation.

Do not weaken architecture guardrails in the epics. If a story seems to need new runtime scope, that scope must first be reflected in PRD and architecture.

### File Structure Requirements

Expected files for this story:

- Update or verify `_bmad-output/planning-artifacts/epics.md`.
- Update this story file only for Dev Agent Record, review findings, and completion status.
- Update `_bmad-output/implementation-artifacts/sprint-status.yaml` through the workflow status transition.

Avoid modifying PRD, architecture, docs, source packages, tests, or samples unless verification finds a direct contradiction that must be corrected for the epics artifact to remain accurate.

### Testing Requirements

This is a documentation/planning artifact story. No runtime test project should be added for Story 1.3.

Recommended verification:

- `ls _bmad-output/planning-artifacts/*epic*.md`
- `rg -n "^\\| FR[0-9]+\\.[0-9]+|^### Story|^Verification:|^Rollback:|^Covers:" _bmad-output/planning-artifacts/epics.md`
- `rg -n "FR1\\.1|FR10\\.5|Story 1\\.3|Documentation cleanup precedes runtime" _bmad-output/planning-artifacts/epics.md _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-18.md`
- `pnpm exec prettier --check _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts/1-3-create-native-epics.md _bmad-output/implementation-artifacts/sprint-status.yaml`
- If any broader planning artifacts change, run `pnpm check` when proportionate.

### Previous Story Intelligence

Story 1.1 completed after review found a missing PRD index and a stale story record. For Story 1.3, prove discovery and file coverage with actual commands before marking tasks done, and keep the File List accurate if `epics.md` changes.

Story 1.2 is now ready for dev and covers architecture verification. Story 1.3 should not depend on Story 1.2 being implemented in this working session, but the developer should use architecture as the source-of-truth input while validating epics.

### Git Intelligence

Recent commits indicate a documentation reset immediately after runtime inbox work. The current branch has many unrelated documentation and scoped reference edits. Keep this story limited to the epics artifact and story tracking unless a broken reference is directly tied to epics discovery.

### Latest Technical Information

No web research is required for this story. It validates local BMAD planning artifacts and FR traceability. Use current repository artifacts rather than external references.

### Project Context Reference

Project context says BMAD epics own implementation sequencing and story acceptance criteria. It also says README files orient humans, AGENTS files orient agents, and GitHub Issues/Projects track backlog work. The developer must not bulk-copy old template implementation code or preserve historical compatibility as a design constraint.

### References

- `_bmad-output/planning-artifacts/epics.md` - Epic 1 and FR Coverage Map
- `_bmad-output/planning-artifacts/prds/prd-Bondstone-2026-06-18/prd.md` - FR1.1, FR1.4, FR10.5
- `_bmad-output/planning-artifacts/architecture.md` - internal runtime and documentation ownership source
- `_bmad-output/planning-artifacts/implementation-readiness-report-2026-06-18.md` - coverage validation and epic quality review
- `_bmad-output/project-context.md` - source-of-truth routing and workflow rules
- `AGENTS.md` - repository operating rules and planned-work routing

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- `ls _bmad-output/planning-artifacts/*epic*.md`
- `find _bmad-output/planning-artifacts -maxdepth 3 -iname '*epic*' -print`
- `rg -n "^\\| FR[0-9]+\\.[0-9]+|^### Story|^Verification:|^Rollback:|^Covers:" _bmad-output/planning-artifacts/epics.md`
- `rg -n "FR1\\.1|FR10\\.5|Story 1\\.3|Documentation cleanup precedes runtime" _bmad-output/planning-artifacts/epics.md _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-18.md`
- `comm -23 /tmp/bondstone-expected-frs.txt /tmp/bondstone-actual-frs.txt`
- `comm -23 /tmp/bondstone-story-refs.txt /tmp/bondstone-story-headings.txt`
- `rg "^### Story" _bmad-output/planning-artifacts/epics.md | wc -l`
- `rg "^Covers:" _bmad-output/planning-artifacts/epics.md | wc -l`
- `rg "^Verification:" _bmad-output/planning-artifacts/epics.md | wc -l`
- `rg "^Rollback:" _bmad-output/planning-artifacts/epics.md | wc -l`
- `rg -n "stable message identities|durable commands|integration events|domain events|outbox|inbox|transport|operation observation|OpenTelemetry|Public API|PostgreSQL|scenario-oriented" _bmad-output/planning-artifacts/epics.md`
- `pnpm exec prettier --check _bmad-output/planning-artifacts/epics.md _bmad-output/implementation-artifacts/1-3-create-native-epics.md _bmad-output/implementation-artifacts/sprint-status.yaml`
- `pnpm check`

### Completion Notes List

- Verified the native BMAD epics artifact exists and is the only epics match under planning artifacts.
- Verified epics front matter derives from the PRD and architecture artifacts.
- Verified all 32 stories include `Covers:`, `Verification:`, and `Rollback:` sections.
- Verified the FR Coverage Map includes all 49 numbered PRD FRs from FR1.1 through FR10.5.
- Verified coverage-map story references resolve to existing story headings.
- Verified documentation cleanup precedes runtime implementation epics and runtime stories preserve scenario-oriented durable-behavior guardrails.
- No additional epics content corrections were required during this verification story; the reviewed epics artifact remains part of the story record.

### File List

- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/implementation-artifacts/1-3-create-native-epics.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-06-18: Verified native epics artifact discovery, derivation, story quality, sequencing, FR traceability, and documentation quality gates.
