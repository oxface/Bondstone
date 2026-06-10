# Documentation Tightening

Goal: turn the MVP-era documentation into durable current-state documentation.

## Scope

- Archive MVP planning documents once their still-current content has been
  moved into stable docs.
- Archive ADR material that is obsolete, removed, or fully superseded when
  keeping it in the active ADR folder harms navigation.
- Choose one ADR archive location, such as `docs/archive/adr/`, and keep the
  convention consistent.
- Move future ideas, deferred possibilities, and speculative design notes into
  `docs/backlog/`.
- Review every stable document for language such as `future`, `planned`,
  `deferred`, `MVP`, and `current slice`; either rewrite it as current state,
  move it to backlog, or archive it.
- Remove direct ADR rule references from AGENTS files where an architecture,
  packaging, testing, repository, sample, or setup doc can carry the current
  rule instead. Keep ADR workflow references where ADR work itself is relevant.
- Audit `docs/setup.md` against the current packages, host composition,
  receive workers, transport adapters, and sample shape.
- Split documents that mix unrelated ownership areas and merge documents that
  force readers to chase one concept through several files.

## Exit Criteria

- Stable docs describe only the current operating contract.
- Historical sequence language lives in ADRs or archive, not in stable docs.
- Future-looking material lives in backlog and is not presented as current
  behavior.
- Root and scoped AGENTS files point agents through stable docs before ADRs,
  except for ADR workflow tasks.
- The docs index accurately describes each document's ownership.

## Handover Prompt

```text
You are taking over Phase 02 of Bondstone's post-MVP stabilization:
Documentation Tightening.

Read first:

- AGENTS.md
- docs/README.md
- docs/backlog/README.md
- docs/backlog/01-adr-application-audit.md
- docs/backlog/02-documentation-tightening.md
- the final report from Phase 01, if present

Mission:

Tighten Bondstone's documentation after the MVP stage. Stable docs should
describe the current operating contract only. Historical sequence, MVP planning
language, deferred possibilities, and speculative future ideas should move to
ADRs, archive, or docs/backlog as appropriate.

Operating rules:

- Do not change durable technical policy without checking whether ADR work is
  required.
- Preserve ADR decision history. Use ADR workflow docs for ADR moves,
  superseding, archiving, or removal.
- Prefer stable docs as the current-rule source for humans and agents.
- AGENTS files should usually point through stable docs, not directly through
  individual ADRs, except for ADR workflow tasks.
- Do not touch unrelated user changes. Check git status before edits and work
  with the current tree.

Execution:

1. Start from the Phase 01 report and confirm the active ADR set has clean
   application states.
2. Inventory docs under docs/, root README.md, root AGENTS.md, scoped AGENTS
   files, and samples/README.md.
3. Search for stale language such as future, planned, deferred, MVP, current
   slice, Rebus, mediator, partially applied, and historical sequence phrasing.
4. For each finding, choose one:
   - rewrite as current state;
   - move speculative work to docs/backlog;
   - move historical planning material to docs/archive;
   - preserve durable decision history in ADRs;
   - remove obsolete duplicated wording.
5. Audit docs/setup.md against the current packages, module registration,
   persistence providers, transport adapters, receive workers, and sample
   shape.
6. Split or merge docs where ownership is unclear, but keep changes reviewable.
7. Update docs/README.md and relevant AGENTS files so navigation matches the
   new documentation ownership.
8. Verify markdown formatting and any affected documentation links.

Deliverables:

- Tightened stable docs that describe current state.
- Archived or backlog-moved planning/speculative material.
- Updated docs index and AGENTS navigation.
- A final documentation report listing:
  - files rewritten;
  - files archived or moved;
  - backlog items created;
  - direct ADR references removed or intentionally kept;
  - verification run;
  - remaining follow-up items for Phase 03.

Phase 02 is complete only when stable docs no longer read like an MVP change
log and future-looking ideas are clearly separated from current guidance.
```
