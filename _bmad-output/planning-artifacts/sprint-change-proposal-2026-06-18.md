---
title: Sprint Change Proposal - Implementation Readiness Corrections
status: applied
date: 2026-06-18
workflowType: sprint-change-proposal
sourceReport: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-18.md
updatedArtifacts:
  - _bmad-output/planning-artifacts/epics.md
---

# Sprint Change Proposal - Implementation Readiness Corrections

## 1. Issue Summary

The implementation readiness report found that the PRD, architecture, and
epics were discoverable, but `epics.md` was not ready to drive full runtime
implementation. The trigger was the readiness assessment dated 2026-06-18.

Core problem:

- `epics.md` did not maintain explicit FR traceability.
- Runtime architecture guardrails from the PRD and architecture were missing
  or only partially represented in stories.
- Epic 5 bundled several unrelated runtime outcomes into a broad milestone.
- Several high-risk runtime acceptance criteria were too general for
  consistent implementation and review.
- Rollback and verification expectations were inconsistent across stories.

Evidence:

- The readiness report identified missing or partial coverage for product
  model, module ownership, command/event/domain-event boundaries, persistence
  transaction boundaries, transport ownership, diagnostics, public API, and
  test-category requirements.
- The readiness report counted 15 missing FR coverage items, 12 partial FR
  coverage items, 4 major epic-quality issues, and 4 minor concerns.
- A follow-up recount of the PRD found 49 numbered FR sub-requirements across
  FR1 through FR10. The original report said 46, so the updated coverage map
  accounts for all 49 numbered FRs as written.

## 2. Impact Analysis

### Epic Impact

Affected epic:

- Previous Epic 5, "Runtime V2 Completion Sequence", was too broad and
  milestone-like.

Updated epic structure:

- Epic 5 now covers runtime module boundaries and message contracts.
- Epic 6 now covers durable persistence and receive ledger behavior.
- Epic 7 now covers transport, operations, and diagnostics.
- Epic 8 now covers public API, verification, and trial readiness.

Existing documentation cleanup epics remain intact, but every story now has
explicit verification and rollback guidance.

### Story Impact

Current and future stories affected:

- Story 1.3 now requires FR traceability, verification guidance, and rollback
  notes.
- Story 1.4 now has explicit verification and rollback sections.
- All existing documentation cleanup stories now include verification and
  rollback sections.
- Former broad runtime stories were replaced by narrower, independently
  verifiable stories covering product boundary, module registration, command
  and query semantics, message-kind separation, outbox atomicity, receive
  transaction boundaries, EF/PostgreSQL migration ownership, transport
  boundaries, operation observation, app-owned retention, OpenTelemetry
  diagnostics, stable setup codes, public API review, package docs, reference
  sweeps, verification gates, and consumer-trial handoff.

### Artifact Conflicts

PRD:

- No PRD edit was required. The PRD already contains the requirements that
  needed traceability.

Architecture:

- No architecture edit was required. The architecture already contains the
  runtime guardrails used to revise `epics.md`.

Epics:

- `epics.md` required direct correction because it lacked explicit FR coverage
  and had oversized runtime implementation stories.

UX:

- No UX artifact is required. UX remains not applicable because Bondstone is a
  library/framework with no UI scope in this change.

### Technical Impact

No runtime code changed as part of this correction. The impact is sequencing
and implementation-readiness only:

- Future runtime implementation should use the new narrower stories.
- Future sprint planning should use the FR Coverage Map in `epics.md`.
- Future verification should use `pnpm check` as the default quality gate and
  `pnpm backend:test:integration` for provider-backed behavior.

## 3. Recommended Approach

Chosen path: Direct Adjustment.

Rationale:

- The PRD and architecture were already aligned, so rollback or MVP reduction
  was unnecessary.
- The issue was localized to epic traceability and story shape.
- Updating `epics.md` preserves the current product scope while making runtime
  implementation safer and more reviewable.

Effort estimate: Medium.

Risk level: Low to Medium.

Timeline impact:

- Documentation-source-of-truth work can continue.
- Runtime implementation should wait until the updated epics are accepted and
  sprint-planned against the narrower story boundaries.

## 4. Detailed Change Proposals

### Epics - FR Traceability

OLD:

```text
No FR Coverage Map was present in epics.md.
```

NEW:

```text
Added an FR Coverage Map that maps FR1.1 through FR10.5 to specific stories.
The map accounts for all 49 numbered PRD FR sub-requirements.
```

Justification:

- Fixes missing traceability and makes PRD coverage auditable before runtime
  implementation.

### Epics - Runtime Structure

OLD:

```text
Epic 5: Runtime V2 Completion Sequence
```

NEW:

```text
Epic 5: Runtime Module Boundaries And Message Contracts
Epic 6: Durable Persistence And Receive Ledger
Epic 7: Transport, Operations, And Diagnostics
Epic 8: Public API, Verification, And Trial Readiness
```

Justification:

- Replaces a broad runtime backlog bucket with narrower outcome epics.

### Stories - Missing Runtime Guardrails

OLD:

```text
Several PRD runtime guardrails had no explicit story acceptance criteria.
```

NEW:

```text
Added or revised stories for:
- product boundary and service-extraction continuity;
- module ownership and AddBondstone composition;
- IModuleCommandExecutor and query boundary semantics;
- durable command send metadata;
- command, integration-event, and domain-event separation;
- source outbox atomicity;
- durable receive transaction boundary and single receive ledger;
- EF/PostgreSQL production persistence and migration ownership;
- thin transport adapters, host-owned topology, and local transport limits;
- operation observation;
- app-owned cleanup, retention, replay, purge, stale recovery, DLQ movement,
  and topology management;
- OpenTelemetry diagnostics and stable setup codes;
- public API classification, package collaboration, and public baseline
  review;
- package policy centralization and xUnit category consistency.
```

Justification:

- Converts architecture guardrails into implementable story criteria.

### Stories - Acceptance Criteria Quality

OLD:

```text
Runtime criteria were mostly broad bullets and often omitted negative cases.
```

NEW:

```text
High-risk runtime stories now use Given/When/Then-style acceptance criteria
for happy path, misconfiguration, duplicate/conflict, transaction boundary,
settlement timing, diagnostic cardinality, and public API compatibility cases.
```

Justification:

- Gives implementers and reviewers crisp pass/fail behavior.

### Stories - Verification And Rollback

OLD:

```text
Rollback notes and verification sections were inconsistent.
```

NEW:

```text
Every story now has a Verification section and a Rollback section.
```

Justification:

- Satisfies FR1.4 and improves sprint review and recovery discipline.

### Verification Gate

OLD:

```text
Epic 6 Story 6.2 did not name pnpm check as the default gate.
```

NEW:

```text
Story 8.4 names pnpm check as the default quality gate and preserves
Integration and Package test routing.
```

Justification:

- Aligns epics with PRD FR10.1 through FR10.5 and `docs/testing.md`.

## 5. Implementation Handoff

Scope classification: Moderate.

Reason:

- The change reorganizes backlog sequencing and acceptance criteria, but does
  not require PRD scope change, architecture change, or runtime code rollback.

Handoff recipients:

- Product Owner / Developer agents: use the revised epics for sprint planning
  and story creation.
- Developer agent: implement runtime work story by story, using the relevant
  architecture and scoped AGENTS files before code changes.
- Test Architect, when invoked: validate story-level verification plans for
  PostgreSQL, transport, public API, and package surfaces.

Success criteria:

- `epics.md` remains discoverable under `_bmad-output/planning-artifacts/`.
- FR Coverage Map covers all numbered PRD FR sub-requirements.
- Runtime implementation starts from the narrower Epic 5 through Epic 8
  stories, not the previous broad Epic 5 bucket.
- Every story has acceptance criteria, verification guidance, and rollback
  notes.
- Future readiness checks no longer report missing or partial FR coverage for
  the issues identified in the 2026-06-18 readiness report.

## 6. Checklist Status

| Checklist item                  | Status     | Notes                                                                     |
| ------------------------------- | ---------- | ------------------------------------------------------------------------- |
| 1.1 Triggering story identified | N/A        | Trigger was readiness report findings, not a single implementation story. |
| 1.2 Core problem defined        | Done       | Epic traceability and story-readiness gaps.                               |
| 1.3 Evidence gathered           | Done       | Readiness report issues and PRD recount.                                  |
| 2.1 Current epic impact         | Done       | Former Epic 5 was split.                                                  |
| 2.2 Required epic-level changes | Done       | Runtime epics reorganized into Epics 5 through 8.                         |
| 2.3 Remaining epic impact       | Done       | Existing documentation epics retained and tightened.                      |
| 2.4 Future epic validity        | Done       | No epic was invalidated; new narrower epics were added.                   |
| 2.5 Epic order and priority     | Done       | Documentation cleanup still precedes runtime implementation.              |
| 3.1 PRD conflicts               | Done       | No PRD conflict found.                                                    |
| 3.2 Architecture conflicts      | Done       | No architecture conflict found.                                           |
| 3.3 UI/UX conflicts             | N/A        | UX remains not applicable.                                                |
| 3.4 Other artifact impacts      | Done       | Verification and docs-routing implications captured.                      |
| 4.1 Direct adjustment           | Viable     | Selected approach.                                                        |
| 4.2 Potential rollback          | Not viable | No completed runtime work needed rollback.                                |
| 4.3 PRD MVP review              | Not viable | MVP scope remains valid.                                                  |
| 4.4 Recommended path            | Done       | Direct adjustment.                                                        |
| 5.1 Issue summary               | Done       | Included above.                                                           |
| 5.2 Impact and artifacts        | Done       | Included above.                                                           |
| 5.3 Path forward                | Done       | Included above.                                                           |
| 5.4 MVP impact and action plan  | Done       | No MVP reduction required.                                                |
| 5.5 Agent handoff               | Done       | Included above.                                                           |
| 6.1 Checklist completion        | Done       | All applicable items addressed.                                           |
| 6.2 Proposal accuracy           | Done       | Proposal reflects applied artifact changes.                               |
| 6.3 User approval               | Done       | User requested batch mode and to fix all found issues.                    |
| 6.4 Sprint status update        | N/A        | No `sprint-status.yaml` exists in the repository.                         |
