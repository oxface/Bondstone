# ADR Application Audit

Goal: make the ADR set trustworthy after the MVP pivot-heavy stage.

## Scope

- Inventory every ADR by status and application state.
- Verify every active accepted or amended ADR against current code, stable
  docs, tests, package boundaries, samples, and agent instructions.
- For each active ADR, either finish applying the decision or move it out of
  active guidance through superseding, archiving, or removing according to the
  ADR workflow.
- Preserve decision traceability for accepted decisions. Prefer superseding
  accepted decisions over deleting them.
- Remove `Pending`, `In Progress`, `Partially Applied`, and `Deferred`
  application states from active accepted ADRs unless a newly accepted ADR
  explicitly keeps a bounded transitional state.
- Make stable docs the current-rule source. ADRs should explain why the rule
  exists, not act as the only place a maintainer can find the rule.

## Exit Criteria

- Every active accepted or amended ADR has `Application: Applied` or
  `Application: Not Applicable`.
- Every superseded, archived, rejected, or removed ADR is clearly out of active
  implementation navigation.
- Stable docs and agent instructions carry the active rules that maintainers
  and agents need.
- The final audit report lists ADRs changed, ADRs left active, ADRs moved out
  of active navigation, and any deliberate follow-up ADRs.

## Handover Prompt

```text
You are taking over Phase 01 of Bondstone's post-MVP stabilization: ADR
Application Audit.

Read first:

- AGENTS.md
- docs/README.md
- docs/adr/README.md
- docs/backlog/README.md
- docs/backlog/01-adr-application-audit.md

Mission:

Perform a deep audit of every ADR under docs/adr. Build an inventory by ADR
number, title, status, application state, and current relevance. Verify each
active accepted or amended ADR against the current code, stable docs, tests,
package boundaries, samples, and agent instructions.

Operating rules:

- Preserve accepted decision history. Do not rewrite accepted Context,
  Decision, or Consequences except for mechanical fixes.
- Prefer superseding accepted decisions over deleting them.
- Archive only when keeping an ADR artifact in active navigation would confuse
  maintainers.
- Remove mistaken never-accepted drafts only when the ADR workflow allows it.
- Stable docs must carry current operating rules. ADRs should explain why the
  rules exist.
- Do not touch unrelated user changes. Check git status before edits and work
  with the current tree.

Execution:

1. Inventory all ADRs and classify them as active, superseded, archived,
   rejected, obsolete, duplicate, or requiring follow-up.
2. For every active accepted or amended ADR, verify whether its application is
   actually complete.
3. For any active ADR with Pending, In Progress, Partially Applied, or Deferred
   application state, choose one of:
   - finish applying it into stable docs, agent instructions, tests, or code;
   - supersede it with a new ADR if the decision changed;
   - archive it if it should no longer steer active work;
   - remove it only if it is a mistaken never-accepted draft.
4. Apply stable-doc and AGENTS updates needed by the ADR changes.
5. Keep edits small and reviewable. If a broad technical decision is discovered,
   create or update ADRs according to docs/adr/README.md instead of burying the
   decision in stable docs.
6. Verify markdown formatting and any affected tests or build commands that are
   relevant to the edits.

Deliverables:

- Updated ADR files and stable docs as needed.
- A final audit report listing:
  - ADRs left active and applied;
  - ADRs superseded;
  - ADRs archived;
  - ADRs removed;
  - stable docs and AGENTS files updated;
  - verification run;
  - remaining follow-up items for Phase 02.

Phase 01 is complete only when every active accepted or amended ADR has
Application: Applied or Application: Not Applicable, and obsolete/deferred
material no longer sits in active ADR navigation.
```
