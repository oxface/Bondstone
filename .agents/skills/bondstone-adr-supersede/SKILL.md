---
name: bondstone-adr-supersede
description: Supersede a Bondstone ADR with a new decision and update stable docs, AGENTS files, and skills.
---

# Bondstone ADR Supersede

Use this skill when an accepted ADR is no longer the current decision and a new
ADR should replace it.

## Read First

- [AGENTS.md](../../../AGENTS.md)
- [docs/README.md](../../../docs/README.md)
- [docs/adr/README.md](../../../docs/adr/README.md)
- The ADR being superseded
- Stable docs, AGENTS files, and skills affected by the old and new decisions

## Workflow

1. Create a new ADR using the next ADR number and the template in
   [docs/adr/README.md](../../../docs/adr/README.md).
2. In the new ADR, link back to the superseded ADR and explain why the old
   decision is being replaced.
3. Mark the old ADR status as `Superseded`.
4. Set the old ADR application state to `Not Applicable` unless it still
   describes residual applied state that must be migrated.
5. Add a link from the old ADR to the new ADR without rewriting the old
   accepted `Context`, `Decision`, or `Consequences` sections.
6. Set the new ADR application state to match reality: `Applied`, `Pending`,
   `In Progress`, `Partially Applied`, or `Deferred`.
7. Identify affected durable docs, AGENTS files, and skills using the mapping
   in [docs/adr/README.md](../../../docs/adr/README.md).
8. Update stable docs so they describe only the current operating rule when
   application has begun or completed. If no suitable doc exists, create the
   smallest useful doc or record the missing doc in [docs/README.md](../../../docs/README.md).
9. Update affected AGENTS files and skills so agents follow the new rule.
10. Update `Application Notes` and `Verification` in the new ADR.

## Output

Report:

- new ADR path, status, and application state;
- superseded ADR path;
- docs, AGENTS files, and skills updated;
- affected durable docs created, changed, pending, or deferred;
- verification performed;
- any compatibility, migration, pending, or deferred application follow-up.

## Verification

Read both ADRs and every changed stable doc or agent instruction. Check for
stale references to the superseded rule with `rg` when relevant.
