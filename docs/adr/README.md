# Architecture Decision Records

ADRs are the durable decision trail for Bondstone. Use them for technical
decisions that affect public API shape, package boundaries, supported
platforms, provider or transport strategy, migration policy, release policy,
sample architecture, compatibility, or repository workflow.

ADRs answer why. Stable docs answer how the repository currently works.

## Location And Naming

Accepted and proposed ADRs live in this folder.

Use sequential numbering:

```text
0001-short-kebab-title.md
0002-another-decision.md
```

Archive removed or obsolete ADR material under `archive/` only when preserving
it in place would confuse current navigation. Prefer superseding an ADR over
deleting it.

## Statuses

- `Proposed`: under discussion and not yet binding.
- `Accepted`: binding until superseded.
- `Rejected`: considered and intentionally not chosen.
- `Amended`: accepted, with later clarifications recorded in the ADR.
- `Superseded`: replaced by a newer ADR.
- `Archived`: retained for traceability but no longer part of active decision
  navigation.

Do not silently rewrite the meaning of an accepted ADR. Add amendments or
supersede it.

## Change Discipline

ADRs have different edit rules depending on status:

- `Proposed` ADRs are review drafts. Their `Context`, `Decision`, and
  `Consequences` may be edited freely until accepted or rejected.
- `Rejected` ADRs should preserve the rejected option and reason. Only
  mechanical typo, formatting, or broken-link fixes should be made after
  rejection unless a dated note clarifies the rejection.
- `Accepted` and `Amended` ADRs are append-only for decision content. Do not
  rewrite `Context`, `Decision`, or `Consequences` except for mechanical typo,
  formatting, or broken-link fixes that do not change meaning.
- `Applied` ADRs are the strictest accepted ADRs. Change them only by adding a
  dated amendment, superseding them, archiving them, updating
  `Application Notes` or `Verification`, or making mechanical fixes.
- `Partially Applied`, `Pending`, `In Progress`, or `Deferred` ADRs may update
  application evidence and pending-work notes as reality changes, but accepted
  decision content remains append-only.
- Put cross-ADR follow-up into a dated amendment, `Application Notes`, or a
  `Related Decisions` section. Do not insert new decision history into the
  original accepted decision text.
- Use `Amended` only for compatible clarification or incremental narrowing.
  Use `Superseded` when a later decision replaces or contradicts the old one.

Stable docs carry the current operating contract. ADRs preserve the decision
trail. If keeping an old ADR "current" would require rewriting history, update
stable docs and add an amendment or superseding ADR instead.

## Application States

Status tracks the decision lifecycle. Application tracks whether the decision
has been reflected in the repository.

- `Not Applicable`: the decision is not binding or has no implementation/docs
  effect.
- `Pending`: the decision is accepted, but required code, docs, instructions,
  or skills have not been applied yet.
- `In Progress`: application work has started but is incomplete.
- `Applied`: the decision is reflected in the current code, stable docs, and
  agent instructions that need it.
- `Partially Applied`: some required application work is complete and the ADR
  names what remains.
- `Deferred`: the decision is accepted, but application is intentionally
  scheduled for later.

Use `Deferred` as an application state, not as a decision status.
Proposed and rejected ADRs should normally use `Application: Not Applicable`.

## ADR Template

```markdown
# 0000 Title

Status: Proposed
Application: Not Applicable
Date: YYYY-MM-DD

## Context

What forces, constraints, goals, or prior decisions make this decision needed?

## Decision

What are we deciding?

## Consequences

What becomes easier, harder, constrained, or intentionally deferred?

## Related Decisions

- Optional links to ADRs that this decision depends on, amends, narrows, or
  supersedes.

## Application Notes

- Current contract:
- Stable docs:
- Agent guidance:
- Application evidence:
- Pending or deferred:

## Verification

What was checked, or why is no executable check relevant?
```

Use the current UTC date when creating or changing ADR status.

## Required Flow

When creating, updating, superseding, or archiving an ADR:

1. Read root [AGENTS.md](../../AGENTS.md), this file, and relevant stable docs.
2. Identify which stable docs, agent instructions, and skills depend on the
   decision.
3. Make the ADR change with explicit `Status`, `Application`, and decision
   trail.
4. Preserve accepted ADR history according to the change-discipline rules:
   append dated amendments for compatible clarifications, and supersede when a
   decision changes.
5. Apply the current accepted rule into stable docs.
6. Apply agent-facing effects into relevant AGENTS files or skills.
7. Report verification and any deliberately deferred docs.

## Affected Durable Docs

Before editing an ADR, identify the stable docs that should reflect the
accepted current state. If no suitable stable doc exists, either create the
smallest useful doc, add a planned doc to [docs/README.md](../README.md), or mark
`Application` as `Pending`, `Partially Applied`, or `Deferred` and explain what
remains.

Use this mapping as a starting point:

- ADR workflow, statuses, and documentation model:
  - [docs/adr/README.md](README.md)
  - [docs/README.md](../README.md)
  - [AGENTS.md](../../AGENTS.md)
  - [.agents/skills/AGENTS.md](../../.agents/skills/AGENTS.md)
  - affected ADR skills
- Repository layout, automation, hooks, devcontainer, and local tooling:
  - [docs/repository.md](../repository.md)
  - [AGENTS.md](../../AGENTS.md)
- Package IDs, target frameworks, package dependency direction, versioning,
  and NuGet publishing:
  - [docs/packaging.md](../packaging.md)
  - [AGENTS.md](../../AGENTS.md)
- Runtime positioning, package architecture, provider strategy, transport
  strategy, compatibility, and service extraction:
  - [docs/architecture/README.md](../architecture/README.md)
  - [docs/architecture/messaging.md](../architecture/messaging.md)
  - [docs/architecture/persistence.md](../architecture/persistence.md)
  - [docs/architecture/persistence-core.md](../architecture/persistence-core.md)
  - [docs/architecture/persistence-ef-core.md](../architecture/persistence-ef-core.md)
  - [docs/architecture/persistence-postgresql.md](../architecture/persistence-postgresql.md)
  - [docs/extraction.md](../extraction.md)
  - [AGENTS.md](../../AGENTS.md)
- Verification entrypoints, test categories, sample expectations, and
  integration-test requirements:
  - [docs/testing.md](../testing.md)
- Sample application topology and scenario intent:
  - [docs/samples.md](../samples.md)

## Applying ADRs

Every accepted ADR must be reflected in stable docs unless it has no current
operational effect. Use `Application Notes` to describe the durable current
contract, the stable docs and agent guidance that carry that contract, and any
application evidence or deferred work. Do not treat ADRs as an exhaustive
source-file manifest; code files, workflow files, and package metadata move too
often for historical ADRs to stay accurate at that level.

When an accepted ADR is not fully applied, set `Application` to `Pending`,
`In Progress`, `Partially Applied`, or `Deferred`, and state what remains in
`Consequences`, `Application Notes`, or `Verification`.

For accepted ADRs, prefer updating `Application Notes`, `Verification`, or a
dated amendment over editing original decision sections. Preserve historical
`Context`, `Decision`, and `Consequences` text unless the edit is purely
mechanical.

Examples:

- A package boundary ADR should update packaging or architecture docs.
- A provider-support ADR should update architecture, testing, and any provider
  adapter skills.
- An ADR workflow ADR should update this file and ADR skills.

## Superseding ADRs

When one ADR replaces another:

- mark the old ADR `Superseded`;
- link to the new ADR from the old ADR;
- link back to the superseded ADR from the new ADR;
- update stable docs to describe only the current rule;
- update AGENTS files and skills that referenced the old rule.

## Archiving Or Removing

Archive only when an ADR artifact is no longer useful in active navigation.
Do not erase traceability for accepted decisions. If an ADR was accepted, prefer
`Superseded` over removal.

Removal is reserved for mistaken, never-accepted drafts or duplicated artifacts.
When removing a draft, mention the removal in the final response or PR notes.
