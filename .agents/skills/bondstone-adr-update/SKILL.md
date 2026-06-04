---
name: bondstone-adr-update
description: Update an existing Bondstone ADR without hiding the decision history, then apply current rules into docs and agent instructions.
---

# Bondstone ADR Update

Use this skill when an existing ADR needs clarification, correction, or an
explicit amendment that does not replace the decision.

## Read First

- [AGENTS.md](../../../AGENTS.md)
- [docs/README.md](../../../docs/README.md)
- [docs/adr/README.md](../../../docs/adr/README.md)
- The ADR being updated
- Stable docs, AGENTS files, and skills named by the ADR's `Application Notes`
  or by the durable-doc mapping

## Workflow

1. Determine whether the change is an update, an amendment, or a superseding
   decision. Use `bondstone-adr-supersede` when the old decision is being
   replaced.
2. Preserve the existing decision trail. Do not silently rewrite accepted
   meaning.
3. For accepted ADRs, add a dated amendment section or clearly marked
   clarification.
4. Update status to `Amended` when the accepted ADR gains a material
   clarification.
5. Preserve or update `Application` to match whether the amended decision is
   applied, pending, in progress, partially applied, deferred, or not
   applicable.
6. Re-identify affected durable docs, AGENTS files, and skills using the
   mapping in [docs/adr/README.md](../../../docs/adr/README.md).
7. Apply the current operating rule into stable docs when the application state
   is `Applied` or `Partially Applied`. If no suitable doc exists, create the
   smallest useful doc or record the missing doc in [docs/README.md](../../../docs/README.md).
8. Apply agent-facing effects into relevant AGENTS files or skills.
9. Update `Application Notes` and `Verification`.

## Output

Report:

- ADR path, resulting status, and application state;
- whether the change was a clarification or amendment;
- stable docs or agent instructions updated;
- affected durable docs created, changed, pending, or deferred;
- pending or deferred application work;
- verification performed.

## Verification

Read back the ADR and every changed stable doc or agent instruction. Run
repository checks only when tooling exists and the update affects checked
source or generated documentation.
