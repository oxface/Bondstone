# Agent Skills

This folder contains repository-local agent skills for Bondstone maintainers.
Skills are reusable workflows, not a substitute for durable architecture docs.

## Skill Rules

- If a workflow is repeated or likely to be repeated, make it a skill.
- Keep skills narrow, named after the job they help with, and grounded in
  versioned repository docs.
- Skills may summarize rules, but durable technical decisions must live in ADRs
  and stable docs.
- Skills that change ADRs must also instruct agents to update the stable docs
  and agent instructions affected by the decision.
- Do not create broad catch-all skills. Prefer several small skills with clear
  trigger descriptions.
- Do not embed stale package versions, current dates, or release-state guesses
  in skill bodies. Point to repository files or package metadata instead.
- Do not use SDD/spec workflow skills for Bondstone by default. Bondstone uses
  ADR-led library maintenance unless an accepted ADR changes this.

## Initial Skill Set

Start with ADR workflow skills:

- `bondstone-adr-create`: create a new ADR from context, options, decision, and
  consequences.
- `bondstone-adr-update`: revise an existing ADR without hiding its decision
  history.
- `bondstone-adr-supersede`: mark an ADR superseded by a new ADR and update
  affected docs.
- `bondstone-adr-archive`: archive or remove obsolete ADR material while
  preserving traceability.
- `bondstone-github-issue-workflow`: create, triage, prioritize, work, and
  complete GitHub Issues and Project items using the conventions in
  [docs/github-workflow.md](../../docs/github-workflow.md).

After the ADR skills exist, add implementation-oriented skills only when a
workflow proves reusable, for example:

- package boundary changes;
- provider adapter additions;
- transport adapter additions;
- sample scenario maintenance;
- release and NuGet publishing validation.

## ADR Discovery

Before changing code, automation, docs, or skills, check whether the change
affects public API, package boundaries, target frameworks, provider or
transport support, migration policy, compatibility, release/publishing, sample
architecture, repository workflow, or agent harness behavior. If it does, use
or recommend the relevant ADR workflow skill before making broad changes.

## ADR Skill Contract

Every ADR skill must enforce this flow:

1. Read root [AGENTS.md](../../AGENTS.md), [docs/README.md](../../docs/README.md),
   and [docs/adr/README.md](../../docs/adr/README.md) when present.
2. Identify the stable docs, AGENTS files, and skills affected by the ADR using
   the durable-doc mapping in [docs/adr/README.md](../../docs/adr/README.md).
3. If no suitable stable doc exists for an accepted decision, create the
   smallest useful doc, add a planned doc to [docs/README.md](../../docs/README.md), or mark
   application as pending, partial, or deferred.
4. Change the ADR artifact with explicit status, application state, and
   decision trail.
5. Preserve accepted ADR decision content. Do not rewrite accepted `Context`,
   `Decision`, or `Consequences` except for mechanical fixes; use dated
   amendments for compatible clarification and superseding ADRs for replacement
   decisions.
6. Apply the accepted current state into stable developer docs.
7. Apply the accepted current state into relevant agent instructions or skills.
8. Report verification and any docs intentionally left unchanged.

ADRs should answer why the decision exists. Stable docs should answer how the
repository currently works.

ADR application notes should describe the durable current contract, stable docs,
agent guidance, evidence, and deferred work. They should not try to preserve an
exhaustive list of changed source files, because source files, workflow files,
and package metadata move as the repository evolves.

For accepted ADRs, application notes and verification may be updated as
application state changes, but the original accepted decision text should remain
traceable.

## Skill Shape

Each skill should live at:

```text
.agents/skills/{skill-name}/
  SKILL.md
  agents/openai.yaml
```

`SKILL.md` should contain:

- frontmatter with `name` and `description`;
- a short "Read First" section;
- the workflow steps;
- expected outputs;
- verification guidance.

`agents/openai.yaml` should expose a concise display name, short description,
and default prompt.
