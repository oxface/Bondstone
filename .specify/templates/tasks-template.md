---
description: "Bondstone task list template for feature implementation"
---

# Tasks: [FEATURE NAME]

**Input**: Design documents from `/specs/[###-feature-name]/`

**Prerequisites**: `plan.md` and `spec.md`; optionally `research.md`, `data-model.md`, `contracts/`, and `quickstart.md`

**Tests**: Include test tasks when behavior changes. Use xUnit `Category` values from `docs/testing.md`: `Unit`, `Application`, `Integration`, `Package`.

**Organization**: Group tasks by independently testable user story, then add cross-cutting docs, packaging, and verification work.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files or independent concerns.
- **[Story]**: User story identifier, e.g. `US1`, `US2`; omit only for setup, foundational, docs-only, or verification tasks.
- Include exact file paths in every task.
- Include the expected test category or verification command when the task adds or changes behavior.

## Bondstone Path Conventions

- Package source: `src/Bondstone*`
- Package tests: `tests/Bondstone*.Tests`
- Public API baselines: `tests/Bondstone.PublicApi.Tests/Baselines`
- Samples: `samples/ModularMonolith*`
- Consumer and repository docs: `docs/*.md`
- SpecKit memory and templates: `.specify/memory/`, `.specify/templates/`
- Agent indexes: `AGENTS.md` and scoped `*/AGENTS.md`

<!--
  Replace all sample tasks below. Generated tasks must map to the actual
  package, docs, sample, and test paths from plan.md.
-->

## Phase 1: Setup And Inventory

**Purpose**: Establish exact affected surfaces before implementation.

- [ ] T001 Read governing context: `.specify/memory/constitution.md`, `.specify/memory/project-profile.md`, `docs/architecture.md`, `docs/testing.md`, and the nearest scoped `AGENTS.md`
- [ ] T002 Inventory affected public/protected APIs, package references, docs, tests, and samples for [FEATURE]
- [ ] T003 [P] Capture current focused verification command and expected failing/changed behavior

---

## Phase 2: Foundational Changes

**Purpose**: Shared contracts, configuration, persistence, transport, or docs structure required before user stories.

- [ ] T004 [P] Add or update shared contract/configuration in `src/[package]/[path].cs`
- [ ] T005 [P] Add or update package-local implementation in `src/[package]/[path].cs`
- [ ] T006 Update dependency references in `src/[package]/[package].csproj` only if required
- [ ] T007 Update package ownership docs in `src/[package]/README.md` or scoped `AGENTS.md` if ownership changes

**Checkpoint**: Foundational behavior is ready for user-story implementation.

---

## Phase 3: User Story 1 - [Title] (Priority: P1)

**Goal**: [Observable outcome]

**Independent Test**: [Focused command, e.g. `dotnet test tests/[project]/[project].csproj --configuration Release --filter "Category=Unit"`]

### Tests for User Story 1

- [ ] T008 [P] [US1] Add `Unit` or `Application` coverage in `tests/[project]/[test-file].cs`
- [ ] T009 [P] [US1] Add `Integration` coverage in `tests/[project]/[test-file].cs` if real provider behavior is required

### Implementation for User Story 1

- [ ] T010 [US1] Implement behavior in `src/[package]/[path].cs`
- [ ] T011 [US1] Add validation, setup diagnostics, or error behavior in `src/[package]/[path].cs`
- [ ] T012 [US1] Update docs or package README for consumer-visible behavior in `docs/[doc].md` or `src/[package]/README.md`

**Checkpoint**: User Story 1 is independently testable and complete.

---

## Phase 4: User Story 2 - [Title] (Priority: P2)

**Goal**: [Observable outcome]

**Independent Test**: [Focused command]

### Tests for User Story 2

- [ ] T013 [P] [US2] Add focused coverage in `tests/[project]/[test-file].cs`

### Implementation for User Story 2

- [ ] T014 [US2] Implement behavior in `src/[package]/[path].cs`
- [ ] T015 [US2] Update relevant docs or samples in `docs/[doc].md` or `samples/[sample]/[path].cs`

**Checkpoint**: User Stories 1 and 2 are independently testable.

---

[Add more user story phases as needed.]

---

## Phase N: Public API, Docs, And Package Polish

**Purpose**: Cross-cutting work that must be complete before final verification.

- [ ] TXXX [P] Update `docs/architecture.md` if durable architecture or package-boundary direction changed
- [ ] TXXX [P] Update `docs/packaging.md` and `docs/package-discovery.md` if package IDs, references, or install guidance changed
- [ ] TXXX [P] Update `docs/testing.md` if test categories, commands, or verification policy changed
- [ ] TXXX [P] Update public API baselines in `tests/Bondstone.PublicApi.Tests/Baselines` only after compatibility review
- [ ] TXXX Add or update GitHub issue/project tracking if this creates follow-up work

---

## Final Verification

- [ ] TXXX Run focused test command: `[command]`
- [ ] TXXX Run `pnpm format:check`
- [ ] TXXX Run `pnpm backend:build`
- [ ] TXXX Run `pnpm backend:test`
- [ ] TXXX Run `pnpm backend:test:integration` if provider or broker behavior changed
- [ ] TXXX Run `pnpm backend:pack` if package metadata, artifacts, public API, or NuGet packaging changed
- [ ] TXXX Run `pnpm check` before final handoff when scope or risk warrants the full quality gate

## Dependencies & Execution Order

- Setup and inventory precede foundational changes.
- Foundational package, contract, persistence, transport, or docs changes precede user stories that depend on them.
- User stories should remain independently testable and may proceed in parallel only when they touch different files or isolated concerns.
- Public API, docs, samples, package metadata, and final verification follow the completed user-story scope.

## Notes

- Do not keep sample tasks in generated `tasks.md`.
- Do not use production `InternalsVisibleTo` for package collaboration.
- Do not treat EF Core InMemory as proof of PostgreSQL durability semantics.
- Do not claim broker topology, retry, dead-letter, prefetch, credentials, monitoring, or migrations as Bondstone-owned without an explicit architecture change.
- Prefer focused verification first; expand to the full gate when blast radius warrants it.
