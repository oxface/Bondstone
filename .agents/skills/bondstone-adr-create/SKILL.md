---
name: bondstone-adr-create
description: Create a new Bondstone ADR and apply accepted decisions into stable docs and agent instructions.
---

# Bondstone ADR Create

Use this skill when a durable technical decision needs a new ADR.

## Read First

- [AGENTS.md](../../../AGENTS.md)
- [docs/README.md](../../../docs/README.md)
- [docs/adr/README.md](../../../docs/adr/README.md)
- Stable docs affected by the proposed decision

## Workflow

1. Confirm the decision needs an ADR. Use ADRs for durable technical decisions,
   not tiny implementation details.
2. Find the next ADR number by listing `docs/adr/[0-9][0-9][0-9][0-9]-*.md`.
3. Create `docs/adr/{number}-{short-kebab-title}.md` from the ADR template in
   [docs/adr/README.md](../../../docs/adr/README.md).
4. Record `Status: Proposed` and `Application: Not Applicable` unless the user
   explicitly asks to accept the decision now.
5. Capture context, considered options when useful, the decision, and
   consequences.
6. Identify affected durable docs, AGENTS files, and skills using the mapping
   in [docs/adr/README.md](../../../docs/adr/README.md).
7. Add `Related Decisions` when this ADR depends on, narrows, amends, or
   supersedes prior ADRs.
8. If status is `Accepted`, set `Application` to match reality:
   `Applied`, `Pending`, `In Progress`, `Partially Applied`, or `Deferred`.
9. For accepted ADRs, apply the current rule into stable docs and any affected
   AGENTS files or skills unless the application state explains why it is
   pending or deferred. If no suitable doc exists, create the smallest useful
   doc or record the missing doc in [docs/README.md](../../../docs/README.md).
10. Fill `Application Notes` with the durable current contract, stable docs,
   agent guidance, application evidence, and any pending or deferred work. Do
   not maintain an exhaustive changed-file list in the ADR.
11. Fill `Verification` with docs checks, command checks, or why executable
    verification is not relevant.

## Output

Report:

- ADR path, status, and application state;
- stable docs or agent instructions updated;
- affected durable docs created, changed, pending, or deferred;
- verification performed;
- unresolved questions or deliberately pending/deferred application work.

## Verification

For ADR-only changes, read back the ADR and affected docs. Run formatting or
tests only when repository tooling exists and the change affects checked files.
