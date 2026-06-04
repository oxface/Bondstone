# 0001 Adopt ADR-Led Maintenance

Status: Amended
Application: Applied
Date: 2026-06-03

## Context

Bondstone is being extracted into its own repository and package set. The
repository needs a durable way to decide package boundaries, supported
platforms, provider and transport strategy, compatibility rules, samples,
release workflow, and agent workflow before code starts moving.

Agents and developers will both maintain this repository. They need a shared
source of durable technical truth without forcing every rule into one large
agent instruction file. The repository should therefore use stable docs for
current operating rules, ADRs for decision history, README files for human
orientation, and AGENTS files for scoped agent instructions.

## Decision

Bondstone uses ADR-led maintenance for durable technical decisions.

ADRs are required before broad changes that affect public API, package
boundaries, target frameworks, provider or transport support, migration policy,
compatibility, release or publishing workflow, sample architecture, repository
workflow, or agent harness behavior.

ADRs track two separate concepts:

- `Status`: the decision lifecycle.
- `Application`: whether the decision has been reflected in code, stable docs,
  agent instructions, and skills.

Reusable ADR workflows live as repository-local skills under `.agents/skills`.
The initial skill set covers creating, updating, superseding, and archiving
ADRs. Skills must point back to durable docs instead of becoming a second
architecture source of truth.

Bondstone will not use SDD/spec-driven product workflow by default. Any future
use of such a workflow requires an accepted ADR with a bounded purpose.

## Consequences

Durable decisions are slower to make, but the repository gets a clear decision
trail before code and package structure solidify.

Agents can discover ADR work from the kind of change being requested, not only
from explicit user wording.

Accepted ADRs must be applied into stable docs and relevant agent instructions
instead of remaining isolated design notes.

The ADR process itself can be amended or superseded by later ADRs if it becomes
too heavy or misses an important repository workflow.

## Amendments

### 2026-06-03: Affected Durable Docs

ADR workflows must identify affected stable docs, AGENTS files, and skills
before changing an ADR. If no stable doc exists yet, the workflow should create
the smallest suitable doc, record planned docs, or explicitly mark application
as pending, partial, or deferred.

## Applied To

- Code: Not applicable.
- Stable docs:
  - [docs/README.md](../README.md)
  - [docs/adr/README.md](README.md)
- Agent instructions:
  - [AGENTS.md](../../AGENTS.md)
  - [.agents/skills/AGENTS.md](../../.agents/skills/AGENTS.md)
- Skills:
  - [.agents/skills/bondstone-adr-create/SKILL.md](../../.agents/skills/bondstone-adr-create/SKILL.md)
  - [.agents/skills/bondstone-adr-update/SKILL.md](../../.agents/skills/bondstone-adr-update/SKILL.md)
  - [.agents/skills/bondstone-adr-supersede/SKILL.md](../../.agents/skills/bondstone-adr-supersede/SKILL.md)
  - [.agents/skills/bondstone-adr-archive/SKILL.md](../../.agents/skills/bondstone-adr-archive/SKILL.md)

## Verification

Read back the ADR guide, root AGENTS instructions, skills AGENTS instructions,
and ADR workflow skills. No executable verification applies because this ADR
documents repository process only.
