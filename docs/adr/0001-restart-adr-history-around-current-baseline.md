# 0001 Restart ADR History Around Current Baseline

Status: Accepted
Application: Applied
Date: 2026-06-16

## Context

Bondstone's first public MVP generated a long ADR trail while the project
explored package boundaries, Rebus and direct transport adapters, topology
diagnostics, direct PostgreSQL persistence, module pipeline capabilities,
domain event placement, operation results, and post-MVP simplification.

That trail is useful history, but it is no longer a good active navigation
surface. More than fifty ADRs now include superseded, amended, deferred, and
partially applied decisions. New work should start from the architecture that
is actually applied after the post-MVP simplification, not from every path the
project tried while finding that shape.

The maintainer explicitly approved restarting the ADR history while preserving
traceability.

## Decision

Restart the active ADR sequence from the current applied baseline.

The previous ADR sequence is archived under
`docs/adr/archive/pre-restart-2026-06-16/`. Archived ADR files are retained for
traceability, but they are no longer the active source of current decisions.
Stable docs and the new active ADR set describe the current operating
contract.

The active ADR set starts again at `0001` and records the durable baseline
decisions that future changes should supersede or amend:

- library scope, package surface, and compatibility posture;
- module execution, module boundaries, and domain events;
- EF/PostgreSQL persistence and operation-state semantics;
- transport adapter boundaries;
- samples, testing, packaging, and documentation governance;
- orchestration and workflow ownership.

Future ADRs should supersede these active baseline ADRs when the current rule
changes. The archived pre-restart ADRs should be consulted only when older
context is needed.

## Consequences

Active ADR navigation becomes smaller and easier to reason about. The
repository still preserves the older decision trail, including rejected and
superseded approaches, but future work can cite the clean baseline instead of
chaining through many amendments.

This is intentionally unusual. It trades fine-grained active history for a
clearer post-MVP baseline because the public compatibility burden is still
bounded and the project is primarily a reusable learning-project library.

Archived ADRs are not deleted. They remain available for archaeology and for
understanding why Bondstone moved away from older choices.

## Related Decisions

- Archives the pre-restart ADR sequence in
  [archive/pre-restart-2026-06-16](archive/pre-restart-2026-06-16).

## Application Notes

- Current contract: active ADRs describe the post-MVP baseline; archived ADRs
  preserve the previous decision trail.
- Stable docs: [docs/adr/README.md](README.md) describes the active/archive
  split, and [docs/README.md](../README.md) continues to point stable behavior
  readers toward architecture, packaging, setup, samples, and testing docs.
- Agent guidance: root [AGENTS.md](../../AGENTS.md) continues to require ADR
  review before broad architecture, package, provider, transport, public API,
  compatibility, sample, and release changes.
- Application evidence: previous ADR files were moved to the pre-restart
  archive and the new active baseline ADR set was created.
- Pending or deferred: none.

## Verification

Checked the previous ADR inventory, stable architecture docs, packaging docs,
sample docs, and testing docs before creating the restart baseline.
