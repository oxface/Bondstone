---
name: bondstone-adr-archive
description: Archive or remove obsolete Bondstone ADR material while preserving traceability for accepted decisions.
---

# Bondstone ADR Archive

Use this skill when ADR material should leave active navigation or a mistaken
draft should be removed.

## Read First

- [AGENTS.md](../../../AGENTS.md)
- [docs/README.md](../../../docs/README.md)
- [docs/adr/README.md](../../../docs/adr/README.md)
- The ADR or draft being archived or removed
- Stable docs, AGENTS files, and skills that reference the material

## Workflow

1. Determine whether the ADR was ever accepted.
2. If it was accepted, prefer `Superseded` over archive or removal. Use
   `bondstone-adr-supersede` when there is a replacement decision.
3. Archive only when active navigation would be clearer without the artifact in
   the main ADR list.
4. Move archived material under `docs/adr/archive/` and set status to
   `Archived`.
5. Set application state to `Not Applicable` unless the archived ADR explicitly
   remains linked to current applied behavior.
6. Remove only mistaken, never-accepted drafts or duplicate artifacts.
7. Identify affected durable docs, AGENTS files, and skills using the mapping
   in [docs/adr/README.md](../../../docs/adr/README.md).
8. Update stable docs, AGENTS files, and skills that referenced the archived or
   removed material.
9. Report the traceability path or explain why removal was appropriate.

## Output

Report:

- archived or removed path;
- prior status and application state;
- docs, AGENTS files, and skills updated;
- affected durable docs changed, pending, or deferred;
- verification performed;
- why superseding was not the better fit.

## Verification

Use `rg` to check for stale links or references to moved or removed ADR files.
Read back changed docs and instructions.
